using JB2026.Api.Models;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/sml")]
public sealed class SmlController : ControllerBase
{
    private readonly IQuotationRepository _quotationRepository;
    private readonly ILogger<SmlController> _logger;

    public SmlController(IQuotationRepository quotationRepository, ILogger<SmlController> logger)
    {
        _quotationRepository = quotationRepository;
        _logger = logger;
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(SmlStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<SmlStatsResponse> GetStats([FromQuery] DateOnly startOn, [FromQuery] int days = 31, [FromQuery] int take = 500)
    {
        if (days is <= 0 or > 31)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(days)] = ["Days must be between 1 and 31."]
            }));
        }

        if (take is <= 0 or > 1000)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(take)] = ["Take must be between 1 and 1000."]
            }));
        }

        var rows = _quotationRepository
            .GetRange(startOn, days)
            .Take(take)
            .ToArray();

        var monthly = rows
            .GroupBy(row => new { row.QuotedOn.Year, row.QuotedOn.Month })
            .OrderBy(group => group.Key.Year)
            .ThenBy(group => group.Key.Month)
            .Select(group => new SmlMonthlyStatResponse
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                Count = group.Count(),
                Amount = group.Sum(item => item.TotalCostA),
            })
            .ToArray();

        var topCustomers = rows
            .GroupBy(row => string.IsNullOrWhiteSpace(row.CustomerName) ? "Unknown" : row.CustomerName)
            .Select(group => new SmlTopCustomerResponse
            {
                CustomerName = group.Key,
                Count = group.Count(),
                Amount = group.Sum(item => item.TotalCostA),
            })
            .OrderByDescending(item => item.Amount)
            .ThenBy(item => item.CustomerName)
            .Take(5)
            .ToArray();

        _logger.LogInformation("Computed SML stats with {Rows} rows for {StartOn}+{Days}", rows.Length, startOn, days);

        return Ok(new SmlStatsResponse
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            RowCount = rows.Length,
            TotalAmount = rows.Sum(row => row.TotalCostA),
            Monthly = monthly,
            TopCustomers = topCustomers,
        });
    }
}
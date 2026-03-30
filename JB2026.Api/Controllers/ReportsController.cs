using JB2026.Api.Models;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IQuotationRepository _quotationRepository;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IQuotationRepository quotationRepository, ILogger<ReportsController> logger)
    {
        _quotationRepository = quotationRepository;
        _logger = logger;
    }

    [HttpPost("run")]
    [ProducesResponseType(typeof(ReportRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<ReportRunResponse> Run([FromBody] RunReportRequest request)
    {
        if (request.Days is <= 0 or > 31)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.Days)] = ["Days must be between 1 and 31."]
            }));
        }

        if (request.Take is <= 0 or > 500)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.Take)] = ["Take must be between 1 and 500."]
            }));
        }

        var rows = _quotationRepository
            .GetRange(request.StartOn, request.Days)
            .Take(request.Take)
            .ToArray();

        var totalCostA = rows.Sum(row => row.TotalCostA);
        _logger.LogInformation("Executed report {ReportName} with {Rows} rows", request.ReportName, rows.Length);

        return Ok(new ReportRunResponse
        {
            ReportName = request.ReportName,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            TotalRows = rows.Length,
            TotalCostA = totalCostA,
            Rows = rows,
        });
    }
}
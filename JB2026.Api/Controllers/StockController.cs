using JB2026.Api.Models;
using JB2026.EfCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/stock")]
public sealed class StockController : ControllerBase
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly ILogger<StockController> _logger;

    public StockController(JB5LegacyReadContext readContext, ILogger<StockController> logger)
    {
        _readContext = readContext;
        _logger = logger;
    }

    [HttpGet("products")]
    [ProducesResponseType(typeof(IReadOnlyList<StockProductListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<StockProductListItemResponse>>> GetProducts(
        [FromQuery] string? keyword,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (take is <= 0 or > 500)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(take)] = ["Take must be between 1 and 500."]
            }));
        }

        var normalizedKeyword = keyword?.Trim();

        var query = _readContext.Products
            .AsNoTracking()
            .Where(product => !product.Retired);

        if (!string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            query = query.Where(product =>
                (product.StockNumber ?? string.Empty).Contains(normalizedKeyword) ||
                (product.ProductCode ?? string.Empty).Contains(normalizedKeyword) ||
                (product.ProductName ?? string.Empty).Contains(normalizedKeyword));
        }

        var products = await query
            .OrderBy(product => product.ProductName)
            .Take(take)
            .Select(product => new StockProductListItemResponse
            {
                ProductId = product.ProductId,
                StockNumber = product.StockNumber ?? string.Empty,
                ProductCode = product.ProductCode ?? string.Empty,
                ProductName = product.ProductName ?? string.Empty,
                Balance = product.Balance,
                SellingPrice = product.SellingPrice,
                COGS = product.COGS,
                Remarks = product.Remarks ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Returned {Count} stock products for keyword '{Keyword}' with take {Take}", products.Count, normalizedKeyword ?? string.Empty, take);
        return Ok(products);
    }
}
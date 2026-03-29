using JB2026.EfCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Rest.Controllers;

[ApiController]
[Authorize]
public sealed class SmlCompatibilityController : ControllerBase
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly IConfiguration _configuration;

    public SmlCompatibilityController(JB5LegacyReadContext readContext, IConfiguration configuration)
    {
        _readContext = readContext;
        _configuration = configuration;
    }

    [HttpGet("api/SML/{id:guid}")]
    public async Task<IActionResult> GetSml(Guid id, CancellationToken cancellationToken)
    {
        var list = await _readContext.SmlRtfHeaders
            .AsNoTracking()
            .Where(x => x.HeaderId == id)
            .ToListAsync(cancellationToken);

        return Ok(list);
    }

    [HttpGet("api/SML/{starton:datetime}/{days:int}")]
    public async Task<IActionResult> GetSml(DateTime starton, int days, CancellationToken cancellationToken)
    {
        if (days is <= 0 or > 366)
        {
            return BadRequest("days must be between 1 and 366");
        }

        var upper = starton.Date.AddDays(1);
        var lower = starton.Date.AddDays(-days);

        var list = await _readContext.SmlRtfHeaders
            .AsNoTracking()
            .Where(x => x.CreatedOn < upper && x.CreatedOn > lower)
            .OrderBy(x => x.PurchaseOrder)
            .ToListAsync(cancellationToken);

        return Ok(list);
    }

    [HttpGet("api/SML/Keyword/{keyword}")]
    public async Task<IActionResult> GetSmlByKeyword(string keyword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 3)
        {
            return Ok(Array.Empty<object>());
        }

        var list = await _readContext.SmlRtfHeaders
            .AsNoTracking()
            .Where(x =>
                !x.Retired
                && ((x.PurchaseOrder ?? string.Empty).Contains(keyword)
                    || (x.CustomerPO ?? string.Empty).Contains(keyword)
                    || (x.OriginalPO ?? string.Empty).Contains(keyword)
                    || (x.OriginalSO ?? string.Empty).Contains(keyword)
                    || (x.SalesOrder ?? string.Empty).Contains(keyword)
                    || (x.Remarks ?? string.Empty).Contains(keyword)))
            .OrderBy(x => x.PurchaseOrder)
            .ToListAsync(cancellationToken);

        return Ok(list);
    }

    [HttpGet("api/SML/file/{id:guid}")]
    public async Task<IActionResult> GetSmlFile(Guid id, CancellationToken cancellationToken)
    {
        var fileRoot = _configuration["LegacyFiles:SmlFileRoot"];
        if (string.IsNullOrWhiteSpace(fileRoot))
        {
            return MissingLegacyPathResponse("LegacyFiles:SmlFileRoot");
        }

        var header = await _readContext.SmlRtfHeaders
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.HeaderId == id, cancellationToken);

        if (header is null || string.IsNullOrWhiteSpace(header.RtfFileName))
        {
            return NotFound();
        }

        var path = Path.Combine(fileRoot, header.RtfFileName);
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        var content = await System.IO.File.ReadAllBytesAsync(path, cancellationToken);
        var contentType = Path.GetExtension(path).Equals(".rtf", StringComparison.OrdinalIgnoreCase)
            ? "application/rtf"
            : "application/vnd.ms-excel";

        return File(content, contentType);
    }

    private static ObjectResult MissingLegacyPathResponse(string key)
    {
        return new ObjectResult(new ProblemDetails
        {
            Title = "Not implemented",
            Detail = $"Set configuration key '{key}' to enable this endpoint.",
            Status = StatusCodes.Status501NotImplemented
        })
        {
            StatusCode = StatusCodes.Status501NotImplemented
        };
    }
}

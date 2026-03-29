using System.Security.Claims;
using JB2026.EfCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Rest.Controllers;

[ApiController]
[Authorize]
public sealed class FcmHistoryCompatibilityController : ControllerBase
{
    private const int PageSize = 50;
    private readonly JB5LegacyReadContext _readContext;

    public FcmHistoryCompatibilityController(JB5LegacyReadContext readContext)
    {
        _readContext = readContext;
    }

    [HttpGet("api/FCMHistory")]
    public async Task<IActionResult> GetFcmHistory(CancellationToken cancellationToken)
    {
        var userSid = ResolveCurrentUserSid();
        if (userSid is null)
        {
            return Unauthorized();
        }

        var result = await BuildVisibleQuery(userSid.Value)
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("api/FCMHistory/{page:int}")]
    public async Task<IActionResult> GetFcmHistoryByPage(int page, CancellationToken cancellationToken)
    {
        var userSid = ResolveCurrentUserSid();
        if (userSid is null)
        {
            return Unauthorized();
        }

        var boundedPage = Math.Max(page, 1);
        var max = boundedPage * PageSize;

        var result = await BuildVisibleQuery(userSid.Value)
            .Take(max)
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("api/FCMHistory/{id:guid}")]
    public async Task<IActionResult> GetFcmHistory(Guid id, CancellationToken cancellationToken)
    {
        var item = await _readContext.FCMHistories
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.FCMHistoryId == id, cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    private IQueryable<JB2026.EfCore.Models.FCMHistory> BuildVisibleQuery(Guid userSid)
    {
        var cutoff = DateTime.Now.AddDays(-30);
        var userSidText = userSid.ToString();

        return _readContext.FCMHistories
            .AsNoTracking()
            .Where(f =>
                f.DeliveredOn >= cutoff
                && ((f.Topic ?? string.Empty).Equals("everyone", StringComparison.OrdinalIgnoreCase)
                    || (f.Topic ?? string.Empty).Equals("staffonly", StringComparison.OrdinalIgnoreCase)
                    || (f.UserIdList ?? string.Empty).Contains(userSidText)))
            .OrderByDescending(f => f.DeliveredOn);
    }

    private Guid? ResolveCurrentUserSid()
    {
        var tokenValue = User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(tokenValue, out var sid) ? sid : null;
    }
}

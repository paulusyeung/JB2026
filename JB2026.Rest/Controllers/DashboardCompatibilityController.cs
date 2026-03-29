using System.Security.Claims;
using JB2026.EfCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Rest.Controllers;

[ApiController]
[Authorize]
public sealed class DashboardCompatibilityController : ControllerBase
{
    private const int UserRoleManager = 3;
    private const int UserRoleAdmin = 4;

    private readonly JB5LegacyReadContext _readContext;

    public DashboardCompatibilityController(JB5LegacyReadContext readContext)
    {
        _readContext = readContext;
    }

    [HttpGet("api/Dashboard/StatJob/Staff/{type}")]
    public async Task<IActionResult> GetDashboardStatJobStaff(string type, CancellationToken cancellationToken)
    {
        var currentYear = DateTime.Now.Year;
        var yearFloor = ResolveYearFloor(type, currentYear);
        var (role, alias) = await GetCurrentAccessAsync(cancellationToken);

        var query = _readContext.vwDashboard_StatJob_Staffs
            .AsNoTracking()
            .Where(x => x.Year.HasValue && x.Year.Value >= yearFloor);

        if (role == UserRoleManager && !string.IsNullOrWhiteSpace(alias))
        {
            query = query.Where(x => x.SalesRep == alias);
        }
        else if (role != UserRoleAdmin)
        {
            query = query.Where(x => false);
        }

        var result = await query
            .OrderBy(x => x.SalesRep)
            .ThenBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("api/Dashboard/StatJob/Average/{type}")]
    public async Task<IActionResult> GetDashboardStatJobAverage(string type, CancellationToken cancellationToken)
    {
        var currentYear = DateTime.Now.Year;
        var yearFloor = ResolveYearFloor(type, currentYear);
        var (role, _) = await GetCurrentAccessAsync(cancellationToken);

        var query = _readContext.vwDashboard_StatJob_Averages
            .AsNoTracking()
            .Where(x => x.Year.HasValue && x.Year.Value >= yearFloor);

        if (role == UserRoleManager)
        {
            query = query.Where(x => x.SalesRep == "Average");
        }
        else if (role != UserRoleAdmin)
        {
            query = query.Where(x => false);
        }

        var result = await query
            .OrderBy(x => x.SalesRep)
            .ThenBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("api/Dashboard/StatSML/Order/{type}")]
    public async Task<IActionResult> GetDashboardStatSmlOrder(string type, CancellationToken cancellationToken)
    {
        var currentYear = DateTime.Now.Year;
        var yearFloor = ResolveYearFloor(type, currentYear);
        var (role, alias) = await GetCurrentAccessAsync(cancellationToken);

        var query = _readContext.vwDashboard_StatSML_Orders
            .AsNoTracking()
            .Where(x => x.Year.HasValue && x.Year.Value >= yearFloor);

        if (role == UserRoleManager && !string.IsNullOrWhiteSpace(alias))
        {
            query = query.Where(x => x.OrderedBy == alias);
        }
        else if (role != UserRoleAdmin)
        {
            query = query.Where(x => false);
        }

        var result = await query
            .OrderBy(x => x.OrderedBy)
            .ThenBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("api/Dashboard/StatSML/Invoice/{type}")]
    public async Task<IActionResult> GetDashboardStatSmlInvoice(string type, CancellationToken cancellationToken)
    {
        var currentYear = DateTime.Now.Year;
        var yearFloor = ResolveYearFloor(type, currentYear);
        var (role, _) = await GetCurrentAccessAsync(cancellationToken);

        var query = _readContext.vwDashboard_StatSML_Invoices
            .AsNoTracking()
            .Where(x => x.Year.HasValue && x.Year.Value >= yearFloor);

        if (role == UserRoleManager)
        {
            query = query.Where(x => x.CustomerName == "Average");
        }
        else if (role != UserRoleAdmin)
        {
            query = query.Where(x => false);
        }

        var result = await query
            .OrderBy(x => x.CustomerName)
            .ThenBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    private static int ResolveYearFloor(string type, int currentYear)
    {
        return type.ToLowerInvariant() switch
        {
            "1y" => currentYear - 1,
            "3y" => currentYear - 3,
            "5y" => currentYear - 5,
            _ => currentYear
        };
    }

    private async Task<(int Role, string Alias)> GetCurrentAccessAsync(CancellationToken cancellationToken)
    {
        var sid = ResolveCurrentSid();
        if (sid is null)
        {
            return (0, string.Empty);
        }

        var user = await _readContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserSid == sid.Value || x.UserId == sid.Value, cancellationToken);

        if (user is null)
        {
            return (0, string.Empty);
        }

        var userInfo = await _readContext.UserInfos
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == user.UserId || x.UserId == sid.Value, cancellationToken);

        return (userInfo?.UserRole ?? 0, user.Alias ?? string.Empty);
    }

    private Guid? ResolveCurrentSid()
    {
        var candidate = User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(candidate, out var sid) ? sid : null;
    }
}

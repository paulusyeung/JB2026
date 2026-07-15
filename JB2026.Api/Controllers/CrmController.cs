using System.Security.Claims;
using System.Xml.Linq;
using JB2026.Api.Models;
using JB2026.Api.Services;
using JB2026.Api.Services.TwentyCrm;
using JB2026.EfCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/crm")]
public sealed class CrmController : ControllerBase
{
    [HttpGet("companies")]
    [ProducesResponseType(typeof(IReadOnlyList<CrmCompanyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CrmCompanyResponse>>> GetCompanies(
        [FromServices] ITwentyCrmService twentyCrmService,
        [FromServices] JB5LegacyReadContext readContext,
        [FromQuery] string? lookup,
        CancellationToken cancellationToken = default)
    {
        var currentUserEmail = await ResolveCurrentUserEmailAsync(readContext, cancellationToken);

        var companies = await twentyCrmService.GetCompaniesAsync(currentUserEmail, lookup, cancellationToken);

        return Ok(companies);
    }

    private async Task<string?> ResolveCurrentUserEmailAsync(JB5LegacyReadContext readContext, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;

        var userInfo = await readContext.UserInfos
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (userInfo?.MetadataXml is null)
            return null;

        return ExtractEmailFromMetadata(userInfo.MetadataXml);
    }

    private static string ExtractEmailFromMetadata(string? metadataXml)
    {
        if (string.IsNullOrWhiteSpace(metadataXml))
            return string.Empty;

        try
        {
            var xml = XElement.Parse(metadataXml);
            return xml.Element("Email")?.Value?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}

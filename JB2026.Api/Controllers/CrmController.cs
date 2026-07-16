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

    [HttpGet("companies/{id}")]
    [ProducesResponseType(typeof(CrmCompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CrmCompanyResponse>> GetCompany(
        string id,
        [FromServices] ITwentyCrmService twentyCrmService,
        CancellationToken cancellationToken = default)
    {
        var company = await twentyCrmService.GetCompanyByIdAsync(id, cancellationToken);

        if (company is null)
            return NotFound();

        return Ok(company);
    }

    [HttpPut("companies/{id}")]
    [ProducesResponseType(typeof(CrmCompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CrmCompanyResponse>> UpdateCompany(
        string id,
        [FromBody] UpdateCrmCompanyRequest request,
        [FromServices] ITwentyCrmService twentyCrmService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var company = await twentyCrmService.UpdateCompanyAsync(id, request, cancellationToken);

            if (company is null)
                return NotFound();

            return Ok(company);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("members")]
    [ProducesResponseType(typeof(IReadOnlyList<CrmMemberResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CrmMemberResponse>>> GetMembers(
        [FromServices] ITwentyCrmService twentyCrmService,
        CancellationToken cancellationToken = default)
    {
        var members = await twentyCrmService.GetWorkspaceMembersAsync(cancellationToken);
        return Ok(members);
    }

    [HttpGet("people")]
    [ProducesResponseType(typeof(IReadOnlyList<CrmCatalogItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CrmCatalogItem>>> GetPeople(
        [FromServices] ITwentyCrmService twentyCrmService,
        [FromQuery] string? lookup,
        CancellationToken cancellationToken = default)
    {
        var people = await twentyCrmService.GetPeopleAsync(lookup, cancellationToken);
        return Ok(people);
    }

    [HttpGet("opportunities")]
    [ProducesResponseType(typeof(IReadOnlyList<CrmCatalogItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CrmCatalogItem>>> GetOpportunities(
        [FromServices] ITwentyCrmService twentyCrmService,
        [FromQuery] string? lookup,
        CancellationToken cancellationToken = default)
    {
        var opportunities = await twentyCrmService.GetOpportunitiesAsync(lookup, cancellationToken);
        return Ok(opportunities);
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

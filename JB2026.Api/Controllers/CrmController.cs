using System;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
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

        var companies = await twentyCrmService.GetCompaniesAsync(currentUserEmail, lookup, readContext, cancellationToken);

        return Ok(companies);
    }

    [HttpGet("companies/{id}")]
    [ProducesResponseType(typeof(CrmCompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CrmCompanyResponse>> GetCompany(
        string id,
        [FromServices] ITwentyCrmService twentyCrmService,
        [FromServices] JB5LegacyReadContext readContext,
        CancellationToken cancellationToken = default)
    {
        var company = await twentyCrmService.GetCompanyByIdAsync(id, readContext, cancellationToken);

        if (company is null)
            return NotFound();

        return Ok(company);
    }

    [HttpGet("companies/{id}/timeline")]
    [ProducesResponseType(typeof(IReadOnlyList<CrmTimelineItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CrmTimelineItemResponse>>> GetCompanyTimeline(
        string id,
        [FromServices] ITwentyCrmService twentyCrmService,
        CancellationToken cancellationToken = default)
    {
        var timeline = await twentyCrmService.GetCompanyTimelineAsync(id, cancellationToken);
        return Ok(timeline);
    }

    [HttpGet("migratable-customers")]
    [ProducesResponseType(typeof(IReadOnlyList<CrmMigratableCustomerResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CrmMigratableCustomerResponse>>> GetMigratableCustomers(
        [FromServices] ITwentyCrmService twentyCrmService,
        [FromServices] JB5LegacyReadContext readContext,
        [FromQuery] int? take = null,
        CancellationToken cancellationToken = default)
    {
        if (take is <= 0 or > 5000)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(take)] = ["Take must be between 1 and 5000."]
            }));
        }

        var existingCompanyNames = await twentyCrmService.GetAllCompanyNamesAsync(cancellationToken);

        var rawQuery = readContext.vwCustomerList_Actives
            .AsNoTracking()
            .GroupJoin(
                readContext.Customers.AsNoTracking(),
                customerView => customerView.CustomerId,
                customer => customer.CustomerId,
                (customerView, customerGroup) => new { customerView, customerGroup })
            .SelectMany(
                x => x.customerGroup.DefaultIfEmpty(),
                (x, customer) => new
                {
                    x.customerView.CustomerId,
                    x.customerView.CustomerName,
                    MetadataXml = customer != null ? customer.MetadataXml : null,
                });

        var rows = await rawQuery
            .OrderBy(row => row.CustomerName)
            .ToListAsync(cancellationToken);

        var result = rows
            .Where(row => !string.IsNullOrWhiteSpace(row.CustomerName))
            .Where(row => existingCompanyNames.Count == 0 || !existingCompanyNames.Contains(row.CustomerName!))
            .Take(take ?? int.MaxValue)
            .Select(row => new CrmMigratableCustomerResponse
            {
                CustomerId = row.CustomerId,
                CustomerName = row.CustomerName!,
                BillingSynced = !string.IsNullOrWhiteSpace(TryExtractMetadataCode(row.MetadataXml, "invoiceNinjaClientId")),
                BillingSyncStatus = TryExtractMetadataCode(row.MetadataXml, "invoiceNinjaClientSyncStatus"),
            })
            .ToArray();

        return Ok(result);
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

    [HttpPost("companies")]
    [ProducesResponseType(typeof(CrmCompanyCreatedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CrmCompanyCreatedResponse>> CreateCompany(
        [FromBody] CreateCrmCompanyRequest request,
        [FromServices] ITwentyCrmService twentyCrmService,
        [FromServices] ICustomerStoredProcedureGateway customerGateway,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Company name is required." });
        }

        try
        {
            var created = await twentyCrmService.CreateCompanyAsync(request, cancellationToken);

            if (created is null)
                return BadRequest(new { message = "Failed to create company in Twenty CRM." });

            if (request.CustomerId is { } customerId)
            {
                await FlagCustomerSyncedToCrmAsync(customerId, customerGateway, cancellationToken);
            }

            return Ok(created);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task FlagCustomerSyncedToCrmAsync(
        Guid customerId,
        ICustomerStoredProcedureGateway customerGateway,
        CancellationToken cancellationToken)
    {
        var current = await customerGateway.SelectAsync(customerId, cancellationToken);
        if (current is null)
            return;

        var metadata = MergeMetadataCode(current.MetadataXml, "SyncedToCRM", "1");

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

        await customerGateway.UpdateAsync(
            new UpdateCustomerStoredProcedureRequest(
                CustomerId: current.CustomerId,
                CustomerName: current.CustomerName,
                LoginAccount: current.LoginAccount,
                LoginPassword: current.LoginPassword,
                MetadataXml: metadata,
                CreatedOn: current.CreatedOn,
                CreatedBy: current.CreatedBy,
                ModifiedOn: DateTime.Now,
                ModifiedBy: actorId,
                Retired: current.Retired,
                RetiredOn: current.RetiredOn,
                RetiredBy: current.RetiredBy),
            cancellationToken);
    }

    private static string MergeMetadataCode(string? metadataXml, string key, string value)
    {
        JsonObject root = new();

        if (!string.IsNullOrWhiteSpace(metadataXml))
        {
            try
            {
                var parsed = JsonNode.Parse(metadataXml.Trim()) as JsonObject;
                if (parsed is not null)
                    root = parsed;
            }
            catch
            {
                // Ignore unparseable metadata and rebuild from scratch.
            }
        }

        root[key] = value;
        return root.ToJsonString();
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
    [ProducesResponseType(typeof(IReadOnlyList<CrmPersonResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CrmPersonResponse>>> GetPeople(
        [FromServices] ITwentyCrmService twentyCrmService,
        [FromQuery] string? lookup,
        CancellationToken cancellationToken = default)
    {
        var people = await twentyCrmService.GetPeopleAsync(lookup, cancellationToken);
        return Ok(people);
    }

    [HttpPut("people/{id}")]
    [ProducesResponseType(typeof(CrmPersonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CrmPersonResponse>> UpdatePerson(
        string id,
        [FromBody] UpdateCrmPersonRequest request,
        [FromServices] ITwentyCrmService twentyCrmService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var person = await twentyCrmService.UpdatePersonAsync(id, request, cancellationToken);

            if (person is null)
                return NotFound();

            return Ok(person);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("people")]
    [ProducesResponseType(typeof(CrmPersonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CrmPersonResponse>> CreatePerson(
        [FromBody] UpdateCrmPersonRequest request,
        [FromServices] ITwentyCrmService twentyCrmService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var person = await twentyCrmService.CreatePersonAsync(request, cancellationToken);

            if (person is null)
                return BadRequest(new { message = "Failed to create person in Twenty CRM." });

            return Ok(person);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("tasks/status-options")]
    [ProducesResponseType(typeof(IReadOnlyList<CrmStageOption>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CrmStageOption>>> GetTaskStatusOptions(
        [FromServices] ITwentyCrmService twentyCrmService,
        CancellationToken cancellationToken = default)
    {
        var options = await twentyCrmService.GetTaskStatusOptionsAsync(cancellationToken);
        return Ok(options);
    }

    [HttpGet("tasks")]
    [ProducesResponseType(typeof(IReadOnlyList<CrmTaskResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CrmTaskResponse>>> GetTasks(
        [FromServices] ITwentyCrmService twentyCrmService,
        [FromQuery] string? lookup,
        CancellationToken cancellationToken = default)
    {
        var tasks = await twentyCrmService.GetTasksAsync(lookup, cancellationToken);
        return Ok(tasks);
    }

    [HttpGet("tasks/{id}")]
    [ProducesResponseType(typeof(CrmTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CrmTaskResponse>> GetTask(
        string id,
        [FromServices] ITwentyCrmService twentyCrmService,
        CancellationToken cancellationToken = default)
    {
        var task = await twentyCrmService.GetTaskByIdAsync(id, cancellationToken);

        if (task is null)
            return NotFound();

        return Ok(task);
    }

    [HttpPut("tasks/{id}")]
    [ProducesResponseType(typeof(CrmTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CrmTaskResponse>> UpdateTask(
        string id,
        [FromBody] UpdateCrmTaskRequest request,
        [FromServices] ITwentyCrmService twentyCrmService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var task = await twentyCrmService.UpdateTaskAsync(id, request, cancellationToken);

            if (task is null)
                return NotFound();

            return Ok(task);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("tasks")]
    [ProducesResponseType(typeof(CrmTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CrmTaskResponse>> CreateTask(
        [FromBody] UpdateCrmTaskRequest request,
        [FromServices] ITwentyCrmService twentyCrmService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var task = await twentyCrmService.CreateTaskAsync(request, cancellationToken);

            if (task is null)
                return BadRequest(new { message = "Failed to create task in Twenty CRM." });

            return Ok(task);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("opportunities")]
    [ProducesResponseType(typeof(IReadOnlyList<CrmOpportunityResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CrmOpportunityResponse>>> GetOpportunities(
        [FromServices] ITwentyCrmService twentyCrmService,
        [FromQuery] string? lookup,
        CancellationToken cancellationToken = default)
    {
        var opportunities = await twentyCrmService.GetOpportunitiesAsync(lookup, cancellationToken);
        return Ok(opportunities);
    }

    [HttpGet("opportunities/stage-options")]
    [ProducesResponseType(typeof(IReadOnlyList<CrmStageOption>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CrmStageOption>>> GetOpportunityStageOptions(
        [FromServices] ITwentyCrmService twentyCrmService,
        CancellationToken cancellationToken = default)
    {
        var options = await twentyCrmService.GetOpportunityStageOptionsAsync(cancellationToken);
        return Ok(options);
    }

    [HttpGet("opportunities/{id}")]
    [ProducesResponseType(typeof(CrmOpportunityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CrmOpportunityResponse>> GetOpportunity(
        string id,
        [FromServices] ITwentyCrmService twentyCrmService,
        CancellationToken cancellationToken = default)
    {
        var opportunity = await twentyCrmService.GetOpportunityByIdAsync(id, cancellationToken);

        if (opportunity is null)
            return NotFound();

        return Ok(opportunity);
    }

    [HttpPut("opportunities/{id}")]
    [ProducesResponseType(typeof(CrmOpportunityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CrmOpportunityResponse>> UpdateOpportunity(
        string id,
        [FromBody] UpdateCrmOpportunityRequest request,
        [FromServices] ITwentyCrmService twentyCrmService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var opportunity = await twentyCrmService.UpdateOpportunityAsync(id, request, cancellationToken);

            if (opportunity is null)
                return NotFound();

            return Ok(opportunity);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("opportunities")]
    [ProducesResponseType(typeof(CrmOpportunityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CrmOpportunityResponse>> CreateOpportunity(
        [FromBody] UpdateCrmOpportunityRequest request,
        [FromServices] ITwentyCrmService twentyCrmService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var opportunity = await twentyCrmService.CreateOpportunityAsync(request, cancellationToken);

            if (opportunity is null)
                return BadRequest(new { message = "Failed to create opportunity" });

            return Ok(opportunity);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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

    private static string TryExtractMetadataCode(string? metadataXml, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(metadataXml))
            return string.Empty;

        try
        {
            using var document = JsonDocument.Parse(metadataXml.Trim());
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(propertyName, out var value)
                && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // Fall back to empty when metadata is not valid JSON.
        }

        return string.Empty;
    }
}

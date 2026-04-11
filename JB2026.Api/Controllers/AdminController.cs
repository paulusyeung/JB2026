using JB2026.Api.Models;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly ILegacyIdentityService _legacyIdentityService;

    public AdminController(ILegacyIdentityService legacyIdentityService)
    {
        _legacyIdentityService = legacyIdentityService;
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminUserResponse>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<AdminUserResponse>> GetUsers()
    {
        var users = _legacyIdentityService
            .GetUsers()
            .Select(user => new AdminUserResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Role = user.Role,
            })
            .OrderBy(user => user.Username, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Ok(users);
    }

    [HttpGet("workflows")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminWorkflowListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AdminWorkflowListItemResponse>>> GetWorkflows(
        [FromServices] JB5LegacyReadContext readContext,
        [FromQuery] string? lookup,
        [FromQuery] string? shortcut,
        [FromQuery] int take = 500,
        CancellationToken cancellationToken = default)
    {
        if (take is <= 0 or > 1000)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(take)] = ["Take must be between 1 and 1000."]
            }));
        }

        var normalizedLookup = lookup?.Trim();
        var normalizedShortcut = shortcut?.Trim();

        var query = readContext.Z_Workflows.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedLookup))
        {
            query = query.Where(workflow =>
                (workflow.WorkflowName ?? string.Empty).Contains(normalizedLookup) ||
                (workflow.WorkTitle ?? string.Empty).Contains(normalizedLookup) ||
                (workflow.WorkInstruction ?? string.Empty).Contains(normalizedLookup));
        }

        if (!string.IsNullOrWhiteSpace(normalizedShortcut) && !string.Equals(normalizedShortcut, "All", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(normalizedShortcut, "9", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(workflow =>
                    string.IsNullOrEmpty(workflow.WorkflowName) ||
                    !char.IsLetter(workflow.WorkflowName[0]));
            }
            else
            {
                var c = char.ToUpperInvariant(normalizedShortcut[0]);
                query = query.Where(workflow =>
                    !string.IsNullOrEmpty(workflow.WorkflowName) &&
                    char.ToUpperInvariant(workflow.WorkflowName[0]) == c);
            }
        }

        var workflows = await query
            .OrderBy(workflow => workflow.WorkflowName)
            .Take(take)
            .Select(workflow => new AdminWorkflowListItemResponse
            {
                WorkflowId = workflow.WorkflowId,
                WorkflowName = workflow.WorkflowName ?? string.Empty,
                WorkTitle = workflow.WorkTitle ?? string.Empty,
                WorkInstruction = workflow.WorkInstruction ?? string.Empty,
            })
            .ToListAsync(cancellationToken);

        return Ok(workflows);
    }

    [HttpGet("workflow-forms")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminWorkflowFormListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AdminWorkflowFormListItemResponse>>> GetWorkflowForms(
        [FromServices] JB5LegacyReadContext readContext,
        [FromQuery] string? lookup,
        [FromQuery] int take = 500,
        CancellationToken cancellationToken = default)
    {
        if (take is <= 0 or > 1000)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(take)] = ["Take must be between 1 and 1000."]
            }));
        }

        var normalizedLookup = lookup?.Trim();

        var query = readContext.Z_Forms.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedLookup))
        {
            query = query.Where(form =>
                (form.FormName ?? string.Empty).Contains(normalizedLookup) ||
                (form.FormName_Chs ?? string.Empty).Contains(normalizedLookup) ||
                (form.FormName_Cht ?? string.Empty).Contains(normalizedLookup));
        }

        var forms = await query
            .OrderBy(form => form.FormName)
            .Take(take)
            .Select(form => new AdminWorkflowFormListItemResponse
            {
                FormId = form.FormId,
                FormName = form.FormName ?? string.Empty,
                FormNameChs = form.FormName_Chs ?? string.Empty,
                FormNameCht = form.FormName_Cht ?? string.Empty,
            })
            .ToListAsync(cancellationToken);

        return Ok(forms);
    }
}
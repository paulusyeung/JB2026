using JB2026.Api.Models;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
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

    [HttpGet("order-type/workflows")]
    [ProducesResponseType(typeof(AdminOrderTypeWorkflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminOrderTypeWorkflowResponse>> GetOrderTypeWorkflows(
        [FromServices] JB5LegacyReadContext readContext,
        [FromQuery] int orderType,
        CancellationToken cancellationToken = default)
    {
        if (orderType is < 0 or > 3)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(orderType)] = ["OrderType must be between 0 and 3."]
            }));
        }

        var workflows = await readContext.Z_Workflows
            .AsNoTracking()
            .OrderBy(workflow => workflow.WorkflowName)
            .Select(workflow => new AdminOrderTypeWorkflowItemResponse
            {
                WorkflowId = workflow.WorkflowId,
                WorkflowName = workflow.WorkflowName ?? string.Empty,
            })
            .ToListAsync(cancellationToken);

        var selectedWorkflowIds = await readContext.Z_OrderTypeWorkflows
            .AsNoTracking()
            .Where(mapping => mapping.OrderType == orderType && mapping.WorkflowId.HasValue)
            .OrderBy(mapping => mapping.WorkIndex)
            .Select(mapping => mapping.WorkflowId!.Value)
            .ToListAsync(cancellationToken);

        var workflowById = workflows.ToDictionary(item => item.WorkflowId);

        var selected = selectedWorkflowIds
            .Where(workflowById.ContainsKey)
            .Select(workflowId => workflowById[workflowId])
            .ToList();

        var selectedSet = selected.Select(item => item.WorkflowId).ToHashSet();
        var available = workflows
            .Where(item => !selectedSet.Contains(item.WorkflowId))
            .ToList();

        return Ok(new AdminOrderTypeWorkflowResponse
        {
            AvailableWorkflows = available,
            SelectedWorkflows = selected,
        });
    }

    [HttpPut("order-type/workflows")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateOrderTypeWorkflows(
        [FromServices] JB5LegacyWriteContext writeContext,
        [FromBody] UpdateAdminOrderTypeWorkflowsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.WorkflowIds.Count == 0)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.WorkflowIds)] = ["At least one workflow must be selected."]
            }));
        }

        var distinctWorkflowIds = request.WorkflowIds.Distinct().ToArray();

        var validWorkflowIds = await writeContext.Z_Workflows
            .AsNoTracking()
            .Where(workflow => distinctWorkflowIds.Contains(workflow.WorkflowId))
            .Select(workflow => workflow.WorkflowId)
            .ToListAsync(cancellationToken);

        if (validWorkflowIds.Count != distinctWorkflowIds.Length)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.WorkflowIds)] = ["One or more workflow ids are invalid."]
            }));
        }

        var existingMappings = await writeContext.Z_OrderTypeWorkflows
            .Where(mapping => mapping.OrderType == request.OrderType)
            .ToListAsync(cancellationToken);

        if (existingMappings.Count > 0)
        {
            writeContext.Z_OrderTypeWorkflows.RemoveRange(existingMappings);
        }

        var newMappings = distinctWorkflowIds
            .Select((workflowId, index) => new Z_OrderTypeWorkflow
            {
                OrderTypeWorkflowId = Guid.NewGuid(),
                OrderType = request.OrderType,
                WorkflowId = workflowId,
                WorkIndex = index,
            })
            .ToArray();

        await writeContext.Z_OrderTypeWorkflows.AddRangeAsync(newMappings, cancellationToken);
        await writeContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
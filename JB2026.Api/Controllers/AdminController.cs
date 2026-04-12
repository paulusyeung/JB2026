using JB2026.Api.Models;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Xml.Linq;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/admin")]
public sealed class AdminController : ControllerBase
{
    public AdminController()
    {
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AdminUserResponse>>> GetUsers(
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
        var query = readContext.vwUserList_Actives.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedLookup))
        {
            query = query.Where(user =>
                (user.UserName ?? string.Empty).Contains(normalizedLookup) ||
                (user.UserAlias ?? string.Empty).Contains(normalizedLookup));
        }

        var users = await query
            .OrderBy(user => user.UserAlias)
            .Take(take)
            .Select(user => new AdminUserResponse
            {
                UserId = user.UserId,
                Username = (user.UserName ?? string.Empty).Trim(),
                DisplayName = string.IsNullOrWhiteSpace(user.UserAlias) ? (user.UserName ?? string.Empty).Trim() : user.UserAlias,
                Role = MapUserRole(user.UserRole),
                PrimaryRec = user.PrimaryRec,
                UserAlias = user.UserAlias ?? string.Empty,
                UserPassword = user.UserPassword ?? string.Empty,
                CreatedOn = user.CreatedOn,
                CreatedBy = user.CreatedBy ?? string.Empty,
                ModifiedOn = user.ModifiedOn,
                ModifiedBy = user.ModifiedBy ?? string.Empty,
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    private static string MapUserRole(int role)
    {
        return role switch
        {
            0 => "Guest",
            1 => "Operator",
            2 => "Supervisor",
            3 => "Manager",
            4 => "Admin",
            _ => role.ToString(),
        };
    }

    [HttpGet("suppliers")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminSupplierListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AdminSupplierListItemResponse>>> GetSuppliers(
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

        var rawQuery = readContext.vwSupplierList_Actives
            .AsNoTracking()
            .GroupJoin(
                readContext.Suppliers.AsNoTracking(),
                supplierView => supplierView.SupplierId,
                supplier => supplier.SupplierId,
                (supplierView, supplierGroup) => new { supplierView, supplierGroup })
            .SelectMany(
                x => x.supplierGroup.DefaultIfEmpty(),
                (x, supplier) => new
                {
                    x.supplierView.SupplierId,
                    x.supplierView.SupplierName,
                    x.supplierView.LoginAccount,
                    x.supplierView.LoginPassword,
                    x.supplierView.CreatedOn,
                    x.supplierView.CreatedBy,
                    x.supplierView.ModifiedOn,
                    x.supplierView.ModifiedBy,
                    MetadataXml = supplier != null ? supplier.MetadataXml : null,
                });

        if (!string.IsNullOrWhiteSpace(normalizedLookup))
        {
            rawQuery = rawQuery.Where(row =>
                row.SupplierName.Contains(normalizedLookup) ||
                row.LoginAccount.Contains(normalizedLookup));
        }

        var rows = await rawQuery
            .OrderBy(row => row.SupplierName)
            .Take(take)
            .ToListAsync(cancellationToken);

        var result = rows.Select(row => new AdminSupplierListItemResponse
        {
            SupplierId = row.SupplierId,
            SupplierName = row.SupplierName,
            LoginAccount = row.LoginAccount,
            LoginPassword = row.LoginPassword,
            SupplierCode = TryExtractMetadataCode(row.MetadataXml, "SupplierCode"),
            CreatedOn = row.CreatedOn,
            CreatedBy = row.CreatedBy ?? string.Empty,
            ModifiedOn = row.ModifiedOn,
            ModifiedBy = row.ModifiedBy ?? string.Empty,
        }).ToArray();

        return Ok(result);
    }

    [HttpGet("customers")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminCustomerListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AdminCustomerListItemResponse>>> GetCustomers(
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
                    x.customerView.LoginAccount,
                    x.customerView.LoginPassword,
                    x.customerView.CreatedOn,
                    x.customerView.CreatedBy,
                    x.customerView.ModifiedOn,
                    x.customerView.ModifiedBy,
                    MetadataXml = customer != null ? customer.MetadataXml : null,
                });

        if (!string.IsNullOrWhiteSpace(normalizedLookup))
        {
            rawQuery = rawQuery.Where(row =>
                row.CustomerName.Contains(normalizedLookup) ||
                row.LoginAccount.Contains(normalizedLookup));
        }

        var rows = await rawQuery
            .OrderBy(row => row.CustomerName)
            .Take(take)
            .ToListAsync(cancellationToken);

        var result = rows.Select(row => new AdminCustomerListItemResponse
        {
            CustomerId = row.CustomerId,
            CustomerName = row.CustomerName,
            LoginAccount = row.LoginAccount,
            LoginPassword = row.LoginPassword,
            CustomerCode = TryExtractMetadataCode(row.MetadataXml, "CustomerCode"),
            CreatedOn = row.CreatedOn,
            CreatedBy = row.CreatedBy ?? string.Empty,
            ModifiedOn = row.ModifiedOn,
            ModifiedBy = row.ModifiedBy ?? string.Empty,
        }).ToArray();

        return Ok(result);
    }

    private static string TryExtractMetadataCode(string? metadataXml, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(metadataXml))
        {
            return string.Empty;
        }

        var trimmed = metadataXml.Trim();

        if (TryExtractMetadataCodeFromJson(trimmed, propertyName, out var jsonCode))
        {
            return jsonCode;
        }

        try
        {
            var document = XDocument.Parse(trimmed);
            var codeElement = document
                .Descendants()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, propertyName, StringComparison.OrdinalIgnoreCase));

            if (codeElement is not null)
            {
                return codeElement.Value.Trim();
            }

            var metadataJsonElement = document
                .Descendants()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "MetadataJson", StringComparison.OrdinalIgnoreCase));

            if (metadataJsonElement is not null && TryExtractMetadataCodeFromJson(metadataJsonElement.Value, propertyName, out var nestedJsonCode))
            {
                return nestedJsonCode;
            }
        }
        catch
        {
            // Ignore metadata parse failures and return empty fallback.
        }

        return string.Empty;
    }

    private static bool TryExtractMetadataCodeFromJson(string json, string propertyName, out string customerCode)
    {
        customerCode = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (TryGetMetadataCodeProperty(document.RootElement, propertyName, out customerCode))
            {
                return true;
            }

            if (document.RootElement.TryGetProperty("MetadataJson", out var nestedMetadataJson)
                && nestedMetadataJson.ValueKind == JsonValueKind.String
                && TryExtractMetadataCodeFromJson(nestedMetadataJson.GetString() ?? string.Empty, propertyName, out customerCode))
            {
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool TryGetMetadataCodeProperty(JsonElement element, string propertyName, out string customerCode)
    {
        customerCode = string.Empty;

        foreach (var property in element.EnumerateObject())
        {
            if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            customerCode = property.Value.GetString()?.Trim() ?? string.Empty;
            return true;
        }

        return false;
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
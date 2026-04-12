using JB2026.Api.Models;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
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

    [HttpGet("suppliers/{id:guid}")]
    [ProducesResponseType(typeof(AdminSupplierRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminSupplierRecordResponse>> GetSupplier(
        Guid id,
        [FromServices] ISupplierStoredProcedureGateway supplierGateway,
        CancellationToken cancellationToken = default)
    {
        var supplier = await supplierGateway.SelectAsync(id, cancellationToken);
        if (supplier is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Supplier not found",
                Detail = $"No supplier exists for id '{id}'.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        return Ok(MapToSupplierRecordResponse(supplier));
    }

    [HttpPost("suppliers")]
    [ProducesResponseType(typeof(AdminSupplierRecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminSupplierRecordResponse>> CreateSupplier(
        [FromServices] ISupplierStoredProcedureGateway supplierGateway,
        [FromBody] CreateAdminSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var actorId = ResolveActorId();
        var now = DateTime.Now;

        var createdId = await supplierGateway.InsertAsync(
            new CreateSupplierStoredProcedureRequest(
                SupplierName: request.SupplierName.Trim(),
                LoginAccount: request.LoginAccount.Trim(),
                LoginPassword: request.LoginPassword.Trim(),
                MetadataXml: BuildSupplierMetadataJson(null, request.SupplierCode, request.BillTo, request.ShipToAddresses),
                CreatedOn: now,
                CreatedBy: actorId,
                ModifiedOn: now,
                ModifiedBy: actorId,
                Retired: false,
                RetiredOn: null,
                RetiredBy: null),
            cancellationToken);

        var createdSupplier = await supplierGateway.SelectAsync(createdId, cancellationToken);
        if (createdSupplier is null)
        {
            return StatusCode(StatusCodes.Status201Created, new AdminSupplierRecordResponse
            {
                SupplierId = createdId,
                SupplierName = request.SupplierName.Trim(),
                LoginAccount = request.LoginAccount.Trim(),
                LoginPassword = request.LoginPassword.Trim(),
                SupplierCode = request.SupplierCode.Trim(),
                BillTo = request.BillTo.Trim(),
                ShipToAddresses = request.ShipToAddresses
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.Name) || !string.IsNullOrWhiteSpace(entry.Address))
                    .Select(entry => new AdminSupplierShipToAddressResponse
                    {
                        Name = entry.Name.Trim(),
                        Address = entry.Address.Trim(),
                    })
                    .ToArray(),
                CreatedOn = now,
                CreatedBy = actorId.ToString(),
                ModifiedOn = now,
                ModifiedBy = actorId.ToString(),
            });
        }

        return StatusCode(StatusCodes.Status201Created, MapToSupplierRecordResponse(createdSupplier));
    }

    [HttpPut("suppliers/{id:guid}")]
    [ProducesResponseType(typeof(AdminSupplierRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminSupplierRecordResponse>> UpdateSupplier(
        Guid id,
        [FromServices] ISupplierStoredProcedureGateway supplierGateway,
        [FromBody] UpdateAdminSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var current = await supplierGateway.SelectAsync(id, cancellationToken);
        if (current is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Supplier not found",
                Detail = $"No supplier exists for id '{id}'.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        var actorId = ResolveActorId();

        await supplierGateway.UpdateAsync(
            new UpdateSupplierStoredProcedureRequest(
                SupplierId: id,
                SupplierName: request.SupplierName.Trim(),
                LoginAccount: request.LoginAccount.Trim(),
                LoginPassword: request.LoginPassword.Trim(),
                MetadataXml: BuildSupplierMetadataJson(current.MetadataXml, request.SupplierCode, request.BillTo, request.ShipToAddresses),
                CreatedOn: current.CreatedOn,
                CreatedBy: current.CreatedBy,
                ModifiedOn: DateTime.Now,
                ModifiedBy: actorId,
                Retired: current.Retired,
                RetiredOn: current.RetiredOn,
                RetiredBy: current.RetiredBy),
            cancellationToken);

        var updated = await supplierGateway.SelectAsync(id, cancellationToken);
        return Ok(updated is null ? MapToSupplierRecordResponse(current) : MapToSupplierRecordResponse(updated));
    }

    [HttpDelete("suppliers/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSupplier(
        Guid id,
        [FromServices] ISupplierStoredProcedureGateway supplierGateway,
        CancellationToken cancellationToken = default)
    {
        var supplier = await supplierGateway.SelectAsync(id, cancellationToken);
        if (supplier is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Supplier not found",
                Detail = $"No supplier exists for id '{id}'.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        await supplierGateway.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private static AdminSupplierRecordResponse MapToSupplierRecordResponse(SupplierStoredProcedureRecord supplier)
    {
        var metadata = ParseSupplierMetadata(supplier.MetadataXml);

        return new AdminSupplierRecordResponse
        {
            SupplierId = supplier.SupplierId,
            SupplierName = supplier.SupplierName?.Trim() ?? string.Empty,
            LoginAccount = supplier.LoginAccount?.Trim() ?? string.Empty,
            LoginPassword = supplier.LoginPassword?.Trim() ?? string.Empty,
            SupplierCode = metadata.SupplierCode,
            BillTo = metadata.BillTo,
            ShipToAddresses = metadata.ShipToAddresses,
            CreatedOn = supplier.CreatedOn,
            CreatedBy = supplier.CreatedBy.ToString(),
            ModifiedOn = supplier.ModifiedOn,
            ModifiedBy = supplier.ModifiedBy.ToString(),
        };
    }

    private static (string SupplierCode, string BillTo, IReadOnlyList<AdminSupplierShipToAddressResponse> ShipToAddresses) ParseSupplierMetadata(string? metadataRaw)
    {
        if (string.IsNullOrWhiteSpace(metadataRaw))
        {
            return (string.Empty, string.Empty, []);
        }

        var trimmed = metadataRaw.Trim();

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                var root = document.RootElement;
                var supplierCode = TryGetJsonString(root, "SupplierCode");
                var billTo = TryGetJsonString(root, "BillTo");
                var shipToAddresses = ParseShipToAddresses(root);
                return (supplierCode, billTo, shipToAddresses);
            }
        }
        catch
        {
            // Fallback to XML parsing.
        }

        try
        {
            var xml = XDocument.Parse(trimmed);
            var supplierCode = xml
                .Descendants()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "SupplierCode", StringComparison.OrdinalIgnoreCase))
                ?.Value.Trim() ?? string.Empty;
            var billTo = xml
                .Descendants()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "BillTo", StringComparison.OrdinalIgnoreCase))
                ?.Value.Trim() ?? string.Empty;
            return (supplierCode, billTo, []);
        }
        catch
        {
            return (string.Empty, string.Empty, []);
        }
    }

    private static IReadOnlyList<AdminSupplierShipToAddressResponse> ParseShipToAddresses(JsonElement root)
    {
        JsonElement shipToArray;
        if (root.TryGetProperty("ShipToAddresses", out shipToArray) && shipToArray.ValueKind == JsonValueKind.Array)
        {
            return shipToArray
                .EnumerateArray()
                .Where(entry => entry.ValueKind == JsonValueKind.Object)
                .Select(entry => new AdminSupplierShipToAddressResponse
                {
                    Name = TryGetJsonString(entry, "Name"),
                    Address = TryGetJsonString(entry, "Address"),
                })
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Name) || !string.IsNullOrWhiteSpace(entry.Address))
                .ToArray();
        }

        return [];
    }

    private static string TryGetJsonString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return string.Empty;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString()?.Trim() ?? string.Empty;
        }

        return value.ToString().Trim();
    }

    private static string BuildSupplierMetadataJson(
        string? existingMetadataRaw,
        string supplierCode,
        string billTo,
        IReadOnlyList<AdminSupplierShipToAddressRequest>? shipToAddresses)
    {
        JsonObject root = new();

        if (!string.IsNullOrWhiteSpace(existingMetadataRaw))
        {
            try
            {
                var parsed = JsonNode.Parse(existingMetadataRaw.Trim()) as JsonObject;
                if (parsed is not null)
                {
                    root = parsed;
                }
            }
            catch
            {
                // Ignore parse failures and rebuild metadata from known fields.
            }
        }

        root["SupplierCode"] = supplierCode.Trim();
        root["BillTo"] = billTo.Trim();

        var shipToArray = new JsonArray();
        foreach (var entry in shipToAddresses ?? [])
        {
            var name = entry.Name.Trim();
            var address = entry.Address.Trim();
            if (name.Length == 0 && address.Length == 0)
            {
                continue;
            }

            shipToArray.Add(new JsonObject
            {
                ["Name"] = name,
                ["Address"] = address,
            });
        }

        root["ShipToAddresses"] = shipToArray;
        return root.ToJsonString();
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

    [HttpGet("customers/{id:guid}")]
    [ProducesResponseType(typeof(AdminCustomerRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminCustomerRecordResponse>> GetCustomer(
        Guid id,
        [FromServices] ICustomerStoredProcedureGateway customerGateway,
        CancellationToken cancellationToken = default)
    {
        var customer = await customerGateway.SelectAsync(id, cancellationToken);
        if (customer is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Customer not found",
                Detail = $"No customer exists for id '{id}'.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        return Ok(MapToCustomerRecordResponse(customer));
    }

    [HttpPost("customers")]
    [ProducesResponseType(typeof(AdminCustomerRecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminCustomerRecordResponse>> CreateCustomer(
        [FromServices] ICustomerStoredProcedureGateway customerGateway,
        [FromBody] CreateAdminCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var actorId = ResolveActorId();
        var now = DateTime.Now;

        var createdId = await customerGateway.InsertAsync(
            new CreateCustomerStoredProcedureRequest(
                CustomerName: request.CustomerName.Trim(),
                LoginAccount: request.LoginAccount.Trim(),
                LoginPassword: request.LoginPassword.Trim(),
                MetadataXml: BuildCustomerMetadataJson(null, request.CustomerCode, request.BillTo, request.ShipToAddresses),
                CreatedOn: now,
                CreatedBy: actorId,
                ModifiedOn: now,
                ModifiedBy: actorId,
                Retired: false,
                RetiredOn: null,
                RetiredBy: null),
            cancellationToken);

        var createdCustomer = await customerGateway.SelectAsync(createdId, cancellationToken);
        if (createdCustomer is null)
        {
            return StatusCode(StatusCodes.Status201Created, new AdminCustomerRecordResponse
            {
                CustomerId = createdId,
                CustomerName = request.CustomerName.Trim(),
                LoginAccount = request.LoginAccount.Trim(),
                LoginPassword = request.LoginPassword.Trim(),
                CustomerCode = request.CustomerCode.Trim(),
                BillTo = request.BillTo.Trim(),
                ShipToAddresses = request.ShipToAddresses
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.Name) || !string.IsNullOrWhiteSpace(entry.Address))
                    .Select(entry => new AdminCustomerShipToAddressResponse
                    {
                        Name = entry.Name.Trim(),
                        Address = entry.Address.Trim(),
                    })
                    .ToArray(),
                CreatedOn = now,
                CreatedBy = actorId.ToString(),
                ModifiedOn = now,
                ModifiedBy = actorId.ToString(),
            });
        }

        return StatusCode(StatusCodes.Status201Created, MapToCustomerRecordResponse(createdCustomer));
    }

    [HttpPut("customers/{id:guid}")]
    [ProducesResponseType(typeof(AdminCustomerRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminCustomerRecordResponse>> UpdateCustomer(
        Guid id,
        [FromServices] ICustomerStoredProcedureGateway customerGateway,
        [FromBody] UpdateAdminCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var current = await customerGateway.SelectAsync(id, cancellationToken);
        if (current is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Customer not found",
                Detail = $"No customer exists for id '{id}'.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        var actorId = ResolveActorId();

        await customerGateway.UpdateAsync(
            new UpdateCustomerStoredProcedureRequest(
                CustomerId: id,
                CustomerName: request.CustomerName.Trim(),
                LoginAccount: request.LoginAccount.Trim(),
                LoginPassword: request.LoginPassword.Trim(),
                MetadataXml: BuildCustomerMetadataJson(current.MetadataXml, request.CustomerCode, request.BillTo, request.ShipToAddresses),
                CreatedOn: current.CreatedOn,
                CreatedBy: current.CreatedBy,
                ModifiedOn: DateTime.Now,
                ModifiedBy: actorId,
                Retired: current.Retired,
                RetiredOn: current.RetiredOn,
                RetiredBy: current.RetiredBy),
            cancellationToken);

        var updated = await customerGateway.SelectAsync(id, cancellationToken);
        return Ok(updated is null ? MapToCustomerRecordResponse(current) : MapToCustomerRecordResponse(updated));
    }

    [HttpDelete("customers/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCustomer(
        Guid id,
        [FromServices] ICustomerStoredProcedureGateway customerGateway,
        CancellationToken cancellationToken = default)
    {
        var customer = await customerGateway.SelectAsync(id, cancellationToken);
        if (customer is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Customer not found",
                Detail = $"No customer exists for id '{id}'.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        await customerGateway.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private static AdminCustomerRecordResponse MapToCustomerRecordResponse(CustomerStoredProcedureRecord customer)
    {
        var metadata = ParseCustomerMetadata(customer.MetadataXml);

        return new AdminCustomerRecordResponse
        {
            CustomerId = customer.CustomerId,
            CustomerName = customer.CustomerName?.Trim() ?? string.Empty,
            LoginAccount = customer.LoginAccount?.Trim() ?? string.Empty,
            LoginPassword = customer.LoginPassword?.Trim() ?? string.Empty,
            CustomerCode = metadata.CustomerCode,
            BillTo = metadata.BillTo,
            ShipToAddresses = metadata.ShipToAddresses,
            CreatedOn = customer.CreatedOn,
            CreatedBy = customer.CreatedBy.ToString(),
            ModifiedOn = customer.ModifiedOn,
            ModifiedBy = customer.ModifiedBy.ToString(),
        };
    }

    private static (string CustomerCode, string BillTo, IReadOnlyList<AdminCustomerShipToAddressResponse> ShipToAddresses) ParseCustomerMetadata(string? metadataRaw)
    {
        if (string.IsNullOrWhiteSpace(metadataRaw))
        {
            return (string.Empty, string.Empty, []);
        }

        var trimmed = metadataRaw.Trim();

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                var root = document.RootElement;
                var customerCode = TryGetJsonString(root, "CustomerCode");
                var billTo = TryGetJsonString(root, "BillTo");
                var shipToAddresses = ParseCustomerShipToAddresses(root);
                return (customerCode, billTo, shipToAddresses);
            }
        }
        catch
        {
            // Fallback to XML parsing.
        }

        try
        {
            var xml = XDocument.Parse(trimmed);
            var customerCode = xml
                .Descendants()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "CustomerCode", StringComparison.OrdinalIgnoreCase))
                ?.Value.Trim() ?? string.Empty;
            var billTo = xml
                .Descendants()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "BillTo", StringComparison.OrdinalIgnoreCase))
                ?.Value.Trim() ?? string.Empty;
            return (customerCode, billTo, []);
        }
        catch
        {
            return (string.Empty, string.Empty, []);
        }
    }

    private static IReadOnlyList<AdminCustomerShipToAddressResponse> ParseCustomerShipToAddresses(JsonElement root)
    {
        JsonElement shipToArray;
        if (root.TryGetProperty("ShipToAddresses", out shipToArray) && shipToArray.ValueKind == JsonValueKind.Array)
        {
            return shipToArray
                .EnumerateArray()
                .Where(entry => entry.ValueKind == JsonValueKind.Object)
                .Select(entry => new AdminCustomerShipToAddressResponse
                {
                    Name = TryGetJsonString(entry, "Name"),
                    Address = TryGetJsonString(entry, "Address"),
                })
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Name) || !string.IsNullOrWhiteSpace(entry.Address))
                .ToArray();
        }

        return [];
    }

    private static string BuildCustomerMetadataJson(
        string? existingMetadataRaw,
        string customerCode,
        string billTo,
        IReadOnlyList<AdminCustomerShipToAddressRequest>? shipToAddresses)
    {
        JsonObject root = new();

        if (!string.IsNullOrWhiteSpace(existingMetadataRaw))
        {
            try
            {
                var parsed = JsonNode.Parse(existingMetadataRaw.Trim()) as JsonObject;
                if (parsed is not null)
                {
                    root = parsed;
                }
            }
            catch
            {
                // Ignore parse failures and rebuild metadata from known fields.
            }
        }

        root["CustomerCode"] = customerCode.Trim();
        root["BillTo"] = billTo.Trim();

        var shipToArray = new JsonArray();
        foreach (var entry in shipToAddresses ?? [])
        {
            var name = entry.Name.Trim();
            var address = entry.Address.Trim();
            if (name.Length == 0 && address.Length == 0)
            {
                continue;
            }

            shipToArray.Add(new JsonObject
            {
                ["Name"] = name,
                ["Address"] = address,
            });
        }

        root["ShipToAddresses"] = shipToArray;
        return root.ToJsonString();
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

    [HttpGet("quotation-items")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminQuotationItemListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AdminQuotationItemListItemResponse>>> GetQuotationItems(
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

        var query = readContext.vwQtItemLists.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedLookup))
        {
            query = query.Where(item =>
                (item.ItemNameEn ?? string.Empty).Contains(normalizedLookup) ||
                (item.ItemNameCht ?? string.Empty).Contains(normalizedLookup) ||
                (item.ItemNameChs ?? string.Empty).Contains(normalizedLookup));
        }

        if (!string.IsNullOrWhiteSpace(normalizedShortcut) && !string.Equals(normalizedShortcut, "All", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(normalizedShortcut, "9", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(item =>
                    !string.IsNullOrEmpty(item.ItemNameEn) &&
                    EF.Functions.Like(item.ItemNameEn, "[0-9]%"));
            }
            else
            {
                var startsWith = $"{char.ToUpperInvariant(normalizedShortcut[0])}%";
                query = query.Where(item =>
                    !string.IsNullOrEmpty(item.ItemNameEn) &&
                    EF.Functions.Like(item.ItemNameEn, startsWith));
            }
        }

        var items = await query
            .OrderBy(item => item.Zone)
            .ThenBy(item => item.Index)
            .Take(take)
            .Select(item => new AdminQuotationItemListItemResponse
            {
                ItemId = item.ItemId,
                ItemGroupId = item.ItemGroupId,
                ItemGroupZone = item.ItemGroupZone,
                Zone = item.Zone ?? string.Empty,
                GroupNameEn = item.GroupNameEn ?? string.Empty,
                GroupNameCht = item.GroupNameCht ?? string.Empty,
                GroupNameChs = item.GroupNameChs ?? string.Empty,
                ItemIndex = item.Index,
                ItemNameEn = item.ItemNameEn ?? string.Empty,
                ItemNameCht = item.ItemNameCht ?? string.Empty,
                ItemNameChs = item.ItemNameChs ?? string.Empty,
                Mandatory = item.Mandatory,
                Fixed = item.Fixed,
                UnitCost = item.UnitCost,
                Minimum = item.Minimum ?? string.Empty,
                UnitCostType = item.UnitCostType,
                CostRounding = item.CostRounding,
                CreatedOn = item.CreatedOn,
                CreatedBy = item.CreatedBy ?? string.Empty,
                ModifiedOn = item.ModifiedOn,
                ModifiedBy = item.ModifiedBy ?? string.Empty,
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost("quotation-items")]
    [ProducesResponseType(typeof(AdminQuotationItemListItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminQuotationItemListItemResponse>> CreateQuotationItem(
        [FromServices] JB5LegacyWriteContext legacyContext,
        [FromBody] CreateAdminQuotationItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var group = await legacyContext.QtItemGroups
            .FirstOrDefaultAsync(x => x.ItemGroupId == request.ItemGroupId, cancellationToken);

        if (group is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Item group not found",
                Detail = $"No item group exists for id '{request.ItemGroupId}'.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        var actorId = ResolveActorId();
        var now = DateTime.Now;

        var item = new QtItem
        {
            ItemId = Guid.NewGuid(),
            ItemGroupId = group.ItemGroupId,
            Zone = group.Zone,
            Index = request.ItemIndex,
            ItemNameEn = request.ItemNameEn.Trim(),
            ItemNameCht = request.ItemNameCht.Trim(),
            ItemNameChs = request.ItemNameChs.Trim(),
            Mandatory = request.Mandatory,
            Fixed = request.Fixed,
            UnitCost = request.UnitCost,
            UnitCostType = request.UnitCostType,
            Minimum = request.Minimum.Trim(),
            CostRounding = request.CostRounding,
            CreatedOn = now,
            CreatedBy = actorId,
            ModifiedOn = now,
            ModifiedBy = actorId,
            Retired = false,
        };

        legacyContext.QtItems.Add(item);
        await legacyContext.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, MapToListItemResponse(item, group));
    }

    [HttpPut("quotation-items/{id:guid}")]
    [ProducesResponseType(typeof(AdminQuotationItemListItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminQuotationItemListItemResponse>> UpdateQuotationItem(
        Guid id,
        [FromServices] JB5LegacyWriteContext legacyContext,
        [FromBody] UpdateAdminQuotationItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var item = await legacyContext.QtItems
            .FirstOrDefaultAsync(x => x.ItemId == id, cancellationToken);

        if (item is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Item not found",
                Detail = $"No quotation item exists for id '{id}'.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        var group = await legacyContext.QtItemGroups
            .FirstOrDefaultAsync(x => x.ItemGroupId == request.ItemGroupId, cancellationToken);

        if (group is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Item group not found",
                Detail = $"No item group exists for id '{request.ItemGroupId}'.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        var actorId = ResolveActorId();

        item.ItemGroupId = group.ItemGroupId;
        item.Zone = group.Zone;
        item.Index = request.ItemIndex;
        item.ItemNameEn = request.ItemNameEn.Trim();
        item.ItemNameCht = request.ItemNameCht.Trim();
        item.ItemNameChs = request.ItemNameChs.Trim();
        item.Mandatory = request.Mandatory;
        item.Fixed = request.Fixed;
        item.UnitCost = request.UnitCost;
        item.UnitCostType = request.UnitCostType;
        item.Minimum = request.Minimum.Trim();
        item.CostRounding = request.CostRounding;
        item.ModifiedOn = DateTime.Now;
        item.ModifiedBy = actorId;
        item.Retired = false;

        await legacyContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToListItemResponse(item, group));
    }

    [HttpDelete("quotation-items/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuotationItem(
        Guid id,
        [FromServices] JB5LegacyWriteContext legacyContext,
        CancellationToken cancellationToken = default)
    {
        var item = await legacyContext.QtItems
            .FirstOrDefaultAsync(x => x.ItemId == id, cancellationToken);

        if (item is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Item not found",
                Detail = $"No quotation item exists for id '{id}'.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        var actorId = ResolveActorId();

        if (item.Retired)
        {
            legacyContext.QtItems.Remove(item);
        }
        else
        {
            item.Retired = true;
            item.RetiredOn = DateTime.Now;
            item.RetiredBy = actorId;
        }

        await legacyContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("quotation-item-groups")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminQuotationItemGroupListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AdminQuotationItemGroupListItemResponse>>> GetQuotationItemGroups(
        [FromServices] JB5LegacyReadContext legacyContext,
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

        var groupsQuery = legacyContext.QtItemGroups.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedLookup))
        {
            groupsQuery = groupsQuery.Where(group =>
                group.Zone.Contains(normalizedLookup) ||
                (group.GroupNameEn ?? string.Empty).Contains(normalizedLookup) ||
                (group.GroupNameCht ?? string.Empty).Contains(normalizedLookup) ||
                (group.GroupNameChs ?? string.Empty).Contains(normalizedLookup));
        }

        var groups = await groupsQuery
            .OrderBy(group => group.Zone)
            .ThenBy(group => group.GroupNameEn)
            .Take(take)
            .ToListAsync(cancellationToken);

        var users = await legacyContext.vwUserList_Actives
            .AsNoTracking()
            .Select(user => new
            {
                user.UserId,
                user.UserAlias,
                user.UserName,
            })
            .ToListAsync(cancellationToken);

        var userDisplayNameLookup = users
            .GroupBy(user => user.UserId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var user = group.First();
                    return string.IsNullOrWhiteSpace(user.UserAlias)
                        ? user.UserName ?? string.Empty
                        : user.UserAlias;
                });

        var result = groups
            .Select(group => new AdminQuotationItemGroupListItemResponse
            {
                ItemGroupId = group.ItemGroupId,
                Zone = group.Zone,
                GroupNameEn = group.GroupNameEn ?? string.Empty,
                GroupNameCht = group.GroupNameCht ?? string.Empty,
                GroupNameChs = group.GroupNameChs ?? string.Empty,
                CreatedOn = group.CreatedOn,
                CreatedBy = userDisplayNameLookup.TryGetValue(group.CreatedBy, out var createdByName)
                    ? createdByName
                    : string.Empty,
                ModifiedOn = group.ModifiedOn,
                ModifiedBy = userDisplayNameLookup.TryGetValue(group.ModifiedBy, out var modifiedByName)
                    ? modifiedByName
                    : string.Empty,
            })
            .ToList();

        return Ok(result);
    }

    [HttpPost("quotation-item-groups")]
    [ProducesResponseType(typeof(AdminQuotationItemGroupListItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminQuotationItemGroupListItemResponse>> CreateQuotationItemGroup(
        [FromServices] JB5LegacyWriteContext legacyContext,
        [FromBody] CreateAdminQuotationItemGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var actorId = ResolveActorId();
        var now = DateTime.Now;

        var item = new QtItemGroup
        {
            ItemGroupId = Guid.NewGuid(),
            Zone = request.Zone.Trim().ToUpperInvariant(),
            GroupNameEn = request.GroupNameEn.Trim(),
            GroupNameCht = request.GroupNameCht.Trim(),
            GroupNameChs = request.GroupNameChs.Trim(),
            CreatedOn = now,
            CreatedBy = actorId,
            ModifiedOn = now,
            ModifiedBy = actorId,
            Retired = false,
        };

        legacyContext.QtItemGroups.Add(item);
        await legacyContext.SaveChangesAsync(cancellationToken);

        var result = MapToListItemResponse(item);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("quotation-item-groups/{id:guid}")]
    [ProducesResponseType(typeof(AdminQuotationItemGroupListItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminQuotationItemGroupListItemResponse>> UpdateQuotationItemGroup(
        Guid id,
        [FromServices] JB5LegacyWriteContext legacyContext,
        [FromBody] UpdateAdminQuotationItemGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var item = await legacyContext.QtItemGroups
            .FirstOrDefaultAsync(x => x.ItemGroupId == id, cancellationToken);

        if (item is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Item group not found",
                Detail = $"No item group exists for id '{id}'.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        var actorId = ResolveActorId();

        item.Zone = request.Zone.Trim().ToUpperInvariant();
        item.GroupNameEn = request.GroupNameEn.Trim();
        item.GroupNameCht = request.GroupNameCht.Trim();
        item.GroupNameChs = request.GroupNameChs.Trim();
        item.ModifiedOn = DateTime.Now;
        item.ModifiedBy = actorId;

        await legacyContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToListItemResponse(item));
    }

    [HttpDelete("quotation-item-groups/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuotationItemGroup(
        Guid id,
        [FromServices] JB5LegacyWriteContext legacyContext,
        CancellationToken cancellationToken = default)
    {
        var item = await legacyContext.QtItemGroups
            .FirstOrDefaultAsync(x => x.ItemGroupId == id, cancellationToken);

        if (item is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Item group not found",
                Detail = $"No item group exists for id '{id}'.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        var actorId = ResolveActorId();

        if (item.Retired)
        {
            // Already soft-deleted: hard delete
            legacyContext.QtItemGroups.Remove(item);
        }
        else
        {
            // Soft delete: mark as retired
            item.Retired = true;
            item.RetiredOn = DateTime.Now;
            item.RetiredBy = actorId;
        }

        await legacyContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Guid ResolveActorId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    private static AdminQuotationItemGroupListItemResponse MapToListItemResponse(QtItemGroup item) =>
        new()
        {
            ItemGroupId = item.ItemGroupId,
            Zone = item.Zone,
            GroupNameEn = item.GroupNameEn ?? string.Empty,
            GroupNameCht = item.GroupNameCht ?? string.Empty,
            GroupNameChs = item.GroupNameChs ?? string.Empty,
            CreatedOn = item.CreatedOn,
            CreatedBy = string.Empty,
            ModifiedOn = item.ModifiedOn,
            ModifiedBy = string.Empty,
        };

    private static AdminQuotationItemListItemResponse MapToListItemResponse(QtItem item, QtItemGroup group) =>
        new()
        {
            ItemId = item.ItemId,
            ItemGroupId = item.ItemGroupId,
            ItemGroupZone = group.Zone,
            Zone = item.Zone ?? string.Empty,
            GroupNameEn = group.GroupNameEn ?? string.Empty,
            GroupNameCht = group.GroupNameCht ?? string.Empty,
            GroupNameChs = group.GroupNameChs ?? string.Empty,
            ItemIndex = item.Index,
            ItemNameEn = item.ItemNameEn ?? string.Empty,
            ItemNameCht = item.ItemNameCht ?? string.Empty,
            ItemNameChs = item.ItemNameChs ?? string.Empty,
            Mandatory = item.Mandatory,
            Fixed = item.Fixed,
            UnitCost = item.UnitCost,
            Minimum = item.Minimum ?? string.Empty,
            UnitCostType = item.UnitCostType,
            CostRounding = item.CostRounding,
            CreatedOn = item.CreatedOn,
            CreatedBy = string.Empty,
            ModifiedOn = item.ModifiedOn,
            ModifiedBy = string.Empty,
        };

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
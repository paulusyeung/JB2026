using JB2026.EfCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Rest.Controllers;

[ApiController]
[Authorize]
public sealed class SupplierCompatibilityController : ControllerBase
{
    private readonly JB5LegacyReadContext _readContext;

    public SupplierCompatibilityController(JB5LegacyReadContext readContext)
    {
        _readContext = readContext;
    }

    [HttpGet("api/Supplier/{id:guid}")]
    public async Task<IActionResult> GetSupplier(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await _readContext.Suppliers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.SupplierId == id, cancellationToken);

        return supplier is null ? NotFound() : Ok(supplier);
    }

    [HttpGet("api/Supplier")]
    public async Task<IActionResult> GetSuppliers(CancellationToken cancellationToken)
    {
        var suppliers = await _readContext.vwSupplierLists
            .AsNoTracking()
            .Where(x => !x.Retired)
            .OrderBy(x => x.SupplierName)
            .ToListAsync(cancellationToken);

        return Ok(suppliers);
    }
}

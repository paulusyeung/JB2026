using System.ComponentModel.DataAnnotations;

namespace JB2026.Api.Models;

public sealed class CreateAdminSupplierRequest
{
    [Required]
    [StringLength(64)]
    public string SupplierName { get; init; } = string.Empty;

    [StringLength(64)]
    public string LoginAccount { get; init; } = string.Empty;

    [StringLength(64)]
    public string LoginPassword { get; init; } = string.Empty;

    [StringLength(64)]
    public string SupplierCode { get; init; } = string.Empty;

    [StringLength(4000)]
    public string BillTo { get; init; } = string.Empty;

    [MaxLength(50)]
    public IReadOnlyList<AdminSupplierShipToAddressRequest> ShipToAddresses { get; init; } = [];
}

public sealed class AdminSupplierShipToAddressRequest
{
    [StringLength(128)]
    public string Name { get; init; } = string.Empty;

    [StringLength(4000)]
    public string Address { get; init; } = string.Empty;
}

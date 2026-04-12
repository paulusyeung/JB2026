using System.ComponentModel.DataAnnotations;

namespace JB2026.Api.Models;

public sealed class CreateAdminCustomerRequest
{
    [Required]
    [StringLength(64)]
    public string CustomerName { get; init; } = string.Empty;

    [StringLength(64)]
    public string LoginAccount { get; init; } = string.Empty;

    [StringLength(64)]
    public string LoginPassword { get; init; } = string.Empty;

    [StringLength(64)]
    public string CustomerCode { get; init; } = string.Empty;

    [StringLength(4000)]
    public string BillTo { get; init; } = string.Empty;

    [MaxLength(50)]
    public IReadOnlyList<AdminCustomerShipToAddressRequest> ShipToAddresses { get; init; } = [];
}

public sealed class AdminCustomerShipToAddressRequest
{
    [StringLength(128)]
    public string Name { get; init; } = string.Empty;

    [StringLength(4000)]
    public string Address { get; init; } = string.Empty;
}

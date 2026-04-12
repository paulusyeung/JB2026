namespace JB2026.Api.Models;

public sealed class AdminSupplierRecordResponse
{
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public string LoginAccount { get; init; } = string.Empty;
    public string LoginPassword { get; init; } = string.Empty;
    public string SupplierCode { get; init; } = string.Empty;
    public string BillTo { get; init; } = string.Empty;
    public IReadOnlyList<AdminSupplierShipToAddressResponse> ShipToAddresses { get; init; } = [];
    public DateTime CreatedOn { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime ModifiedOn { get; init; }
    public string ModifiedBy { get; init; } = string.Empty;
}

public sealed class AdminSupplierShipToAddressResponse
{
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
}

namespace JB2026.Api.Models;

public sealed class AdminSupplierListItemResponse
{
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public string LoginAccount { get; init; } = string.Empty;
    public string LoginPassword { get; init; } = string.Empty;
    public string SupplierCode { get; init; } = string.Empty;
    public DateTime CreatedOn { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime ModifiedOn { get; init; }
    public string ModifiedBy { get; init; } = string.Empty;
}

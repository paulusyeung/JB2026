namespace JB2026.Api.Models;

public sealed class AdminCustomerListItemResponse
{
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string LoginAccount { get; init; } = string.Empty;
    public string LoginPassword { get; init; } = string.Empty;
    public string CustomerCode { get; init; } = string.Empty;
    public DateTime CreatedOn { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime ModifiedOn { get; init; }
    public string ModifiedBy { get; init; } = string.Empty;
    public string InvoiceNinjaClientId { get; init; } = string.Empty;
    public string BillingSyncStatus { get; init; } = string.Empty;
}

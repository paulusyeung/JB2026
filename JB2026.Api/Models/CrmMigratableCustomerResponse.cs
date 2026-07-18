namespace JB2026.Api.Models;

public sealed class CrmMigratableCustomerResponse
{
    public Guid CustomerId { get; init; }

    public string CustomerName { get; init; } = string.Empty;

    public bool BillingSynced { get; init; }

    public string BillingSyncStatus { get; init; } = string.Empty;
}

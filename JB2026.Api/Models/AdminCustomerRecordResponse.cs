namespace JB2026.Api.Models;

public sealed class AdminCustomerRecordResponse
{
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string LoginAccount { get; init; } = string.Empty;
    public string LoginPassword { get; init; } = string.Empty;
    public string CustomerCode { get; init; } = string.Empty;
    public string BillTo { get; init; } = string.Empty;
    public string Group { get; init; } = string.Empty;
    public IReadOnlyList<AdminCustomerShipToAddressResponse> ShipToAddresses { get; init; } = [];
    public DateTime CreatedOn { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime ModifiedOn { get; init; }
    public string ModifiedBy { get; init; } = string.Empty;
}

public sealed class AdminCustomerShipToAddressResponse
{
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
}

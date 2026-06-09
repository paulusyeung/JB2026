namespace JB2026.Api.Models;

public sealed class SummarizeCustomerContactResponse
{
    public Guid CustomerId { get; init; }

    public ContactInfoSummary Summary { get; init; } = new();

    public bool Persisted { get; init; }

    public bool ExistingCustomerSummaryPresent { get; init; }

    public string? ErrorMessage { get; init; }
}

namespace JB2026.Api.Models;

public sealed class SettingsResponse
{
    public required string CompanyName { get; init; }

    public required string TimeZone { get; init; }

    public required string CurrencyCode { get; init; }

    public required bool EnableLegacyFallback { get; init; }

    public string OwnerName { get; init; } = string.Empty;

    public string NextOrderNumber { get; init; } = string.Empty;

    public string NextProductNumber { get; init; } = string.Empty;

    public string NextQuotationNumber { get; init; } = string.Empty;

    public int CommonQueryIndex { get; init; }

    public int CompletedQueryIndex { get; init; }

    public int ScheduleQueryRange { get; init; }

    public string GmailAccount { get; init; } = string.Empty;

    public string GmailPassword { get; init; } = string.Empty;
}
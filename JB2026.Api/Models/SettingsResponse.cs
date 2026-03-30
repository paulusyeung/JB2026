namespace JB2026.Api.Models;

public sealed class SettingsResponse
{
    public required string CompanyName { get; init; }

    public required string TimeZone { get; init; }

    public required string CurrencyCode { get; init; }

    public required bool EnableLegacyFallback { get; init; }
}
using System.ComponentModel.DataAnnotations;

namespace JB2026.Api.Models;

public sealed class UpdateSettingsRequest
{
    [Required]
    [StringLength(128)]
    public string CompanyName { get; init; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string TimeZone { get; init; } = string.Empty;

    [Required]
    [StringLength(8)]
    public string CurrencyCode { get; init; } = string.Empty;

    public bool EnableLegacyFallback { get; init; }
}
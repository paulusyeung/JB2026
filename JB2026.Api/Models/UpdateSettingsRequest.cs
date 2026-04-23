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

    [Required]
    [StringLength(128)]
    public string OwnerName { get; init; } = string.Empty;

    [Required]
    [StringLength(16)]
    public string NextOrderNumber { get; init; } = string.Empty;

    [Required]
    [StringLength(16)]
    public string NextProductNumber { get; init; } = string.Empty;

    [Required]
    [StringLength(16)]
    public string NextQuotationNumber { get; init; } = string.Empty;

    [Range(0, 10)]
    public int CommonQueryIndex { get; init; }

    [Range(0, 10)]
    public int CompletedQueryIndex { get; init; }

    [Range(1, 3650)]
    public int ScheduleQueryRange { get; init; }

    [StringLength(255)]
    public string GmailAccount { get; init; } = string.Empty;

    [StringLength(255)]
    public string GmailPassword { get; init; } = string.Empty;

    [StringLength(32)]
    public string DateFormatPreference { get; init; } = SettingsResponse.DefaultDateFormatPreference;
}
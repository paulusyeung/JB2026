using System.ComponentModel.DataAnnotations;

namespace JB2026.Api.Models;

public sealed class CreateAdminQuotationItemGroupRequest
{
    [Required]
    [StringLength(1, MinimumLength = 1)]
    public string Zone { get; init; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string GroupNameEn { get; init; } = string.Empty;

    [StringLength(64)]
    public string GroupNameCht { get; init; } = string.Empty;

    [StringLength(64)]
    public string GroupNameChs { get; init; } = string.Empty;
}

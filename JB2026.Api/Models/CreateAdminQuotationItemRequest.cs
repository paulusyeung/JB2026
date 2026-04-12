using System.ComponentModel.DataAnnotations;

namespace JB2026.Api.Models;

public sealed class CreateAdminQuotationItemRequest
{
    [Required]
    public Guid ItemGroupId { get; init; }

    public int ItemIndex { get; init; }

    [StringLength(64)]
    public string ItemNameEn { get; init; } = string.Empty;

    [StringLength(64)]
    public string ItemNameCht { get; init; } = string.Empty;

    [StringLength(64)]
    public string ItemNameChs { get; init; } = string.Empty;

    public bool Mandatory { get; init; }

    public bool Fixed { get; init; }

    public decimal UnitCost { get; init; }

    [Range(0, 6)]
    public int UnitCostType { get; init; }

    [StringLength(32)]
    public string Minimum { get; init; } = string.Empty;

    public decimal CostRounding { get; init; }
}
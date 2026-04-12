namespace JB2026.Api.Models;

public sealed class AdminQuotationItemListItemResponse
{
    public Guid ItemId { get; init; }
    public Guid ItemGroupId { get; init; }
    public string ItemGroupZone { get; init; } = string.Empty;
    public string Zone { get; init; } = string.Empty;
    public string GroupNameEn { get; init; } = string.Empty;
    public string GroupNameCht { get; init; } = string.Empty;
    public string GroupNameChs { get; init; } = string.Empty;
    public int ItemIndex { get; init; }
    public string ItemNameEn { get; init; } = string.Empty;
    public string ItemNameCht { get; init; } = string.Empty;
    public string ItemNameChs { get; init; } = string.Empty;
    public bool Mandatory { get; init; }
    public bool Fixed { get; init; }
    public decimal UnitCost { get; init; }
    public string Minimum { get; init; } = string.Empty;
    public int UnitCostType { get; init; }
    public decimal CostRounding { get; init; }
    public DateTime CreatedOn { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime ModifiedOn { get; init; }
    public string ModifiedBy { get; init; } = string.Empty;
}
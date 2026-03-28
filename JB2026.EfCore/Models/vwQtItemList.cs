using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwQtItemList
{
    public Guid ItemGroupId { get; set; }

    public string ItemGroupZone { get; set; } = null!;

    public string? GroupNameEn { get; set; }

    public string? GroupNameCht { get; set; }

    public string? GroupNameChs { get; set; }

    public Guid ItemId { get; set; }

    public string? ItemNameEn { get; set; }

    public string? ItemNameCht { get; set; }

    public string? ItemNameChs { get; set; }

    public bool Mandatory { get; set; }

    public string? Zone { get; set; }

    public int Index { get; set; }

    public bool Fixed { get; set; }

    public string? Minimum { get; set; }

    public decimal UnitCost { get; set; }

    public int UnitCostType { get; set; }

    public decimal CostRounding { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public bool Retired { get; set; }

    public DateTime? RetiredOn { get; set; }

    public string? RetiredBy { get; set; }
}

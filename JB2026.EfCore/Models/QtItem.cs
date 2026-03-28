using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class QtItem
{
    public Guid ItemId { get; set; }

    public Guid ItemGroupId { get; set; }

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

    public decimal MinimumCost { get; set; }

    public decimal CostRounding { get; set; }

    public DateTime CreatedOn { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public Guid ModifiedBy { get; set; }

    public bool Retired { get; set; }

    public DateTime? RetiredOn { get; set; }

    public Guid? RetiredBy { get; set; }

    public virtual QtItemGroup ItemGroup { get; set; } = null!;

    public virtual ICollection<QtDetail> QtDetails { get; set; } = new List<QtDetail>();
}

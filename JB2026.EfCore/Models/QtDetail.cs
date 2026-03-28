using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class QtDetail
{
    public Guid DetailId { get; set; }

    public Guid HeaderId { get; set; }

    public Guid? ItemId { get; set; }

    public string? Zone { get; set; }

    public int? Index { get; set; }

    public string? Description { get; set; }

    public string? Minimum { get; set; }

    public decimal? UnitCost { get; set; }

    public decimal? CostA { get; set; }

    public decimal? CostB { get; set; }

    public decimal? CostC { get; set; }

    public decimal? CostD { get; set; }

    public virtual QtHeader Header { get; set; } = null!;

    public virtual QtItem? Item { get; set; }
}

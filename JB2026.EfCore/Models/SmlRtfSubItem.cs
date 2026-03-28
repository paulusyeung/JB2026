using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class SmlRtfSubItem
{
    public Guid SubItemId { get; set; }

    public Guid ItemId { get; set; }

    public int SubLineNumber { get; set; }

    public string? Start_End { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? LabelSize { get; set; }

    public string? Qty { get; set; }

    public virtual SmlRtfItem Item { get; set; } = null!;
}

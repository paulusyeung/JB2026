using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class SmlRtfItem
{
    public Guid ItemId { get; set; }

    public Guid HeaderId { get; set; }

    public int LineNumber { get; set; }

    public string? ProductCode { get; set; }

    public string? ProductDescription { get; set; }

    public string? Price { get; set; }

    public string? Discount { get; set; }

    public string? Qty { get; set; }

    public string? Amount { get; set; }

    public string? PostProcess { get; set; }

    public virtual SmlRtfHeader Header { get; set; } = null!;

    public virtual ICollection<SmlRtfSubItem> SmlRtfSubItems { get; set; } = new List<SmlRtfSubItem>();
}

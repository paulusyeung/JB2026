using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwRtfItemList
{
    public Guid HeaderId { get; set; }

    public string? PurchaseOrder { get; set; }

    public string CustomerPO { get; set; } = null!;

    public DateTime OrderedOn { get; set; }

    public string? OrderedBy { get; set; }

    public string? OriginalPO { get; set; }

    public string? SalesOrder { get; set; }

    public string OriginalSO { get; set; } = null!;

    public Guid ItemId { get; set; }

    public int LineNumber { get; set; }

    public string? ProductCode { get; set; }

    public string? ProductDescription { get; set; }

    public string? Price { get; set; }

    public string? Discount { get; set; }

    public string? Qty { get; set; }

    public string? Amount { get; set; }
}

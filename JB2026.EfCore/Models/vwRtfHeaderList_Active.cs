using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwRtfHeaderList_Active
{
    public Guid HeaderId { get; set; }

    public string? RtfFileName { get; set; }

    public string? PurchaseOrder { get; set; }

    public string CustomerPO { get; set; } = null!;

    public DateTime OrderedOn { get; set; }

    public string? OrderedBy { get; set; }

    public string? OriginalPO { get; set; }

    public string? SalesOrder { get; set; }

    public string OriginalSO { get; set; } = null!;

    public string? Remarks { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string? ModifiedBy { get; set; }
}

using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class SmlRtfHeader
{
    public Guid HeaderId { get; set; }

    public string? RtfFileName { get; set; }

    public string? PurchaseOrder { get; set; }

    public string? CustomerPO { get; set; }

    public DateTime OrderedOn { get; set; }

    public string? OrderedBy { get; set; }

    public string? OriginalPO { get; set; }

    public string? SalesOrder { get; set; }

    public string? OriginalSO { get; set; }

    public string? Remarks { get; set; }

    public DateTime CreatedOn { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public Guid ModifiedBy { get; set; }

    public bool Retired { get; set; }

    public DateTime RetiredOn { get; set; }

    public Guid? RetiredBy { get; set; }

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public virtual ICollection<SmlRtfExtractToDN> SmlRtfExtractToDNs { get; set; } = new List<SmlRtfExtractToDN>();

    public virtual ICollection<SmlRtfItem> SmlRtfItems { get; set; } = new List<SmlRtfItem>();
}

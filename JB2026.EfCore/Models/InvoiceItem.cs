using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class InvoiceItem
{
    public Guid ItemId { get; set; }

    public Guid HeaderId { get; set; }

    public Guid? SmlRtfHeaderId { get; set; }

    public int LineNumber { get; set; }

    public string? Notes { get; set; }

    public virtual InvoiceHeader Header { get; set; } = null!;

    public virtual ICollection<InvoiceSubItem> InvoiceSubItems { get; set; } = new List<InvoiceSubItem>();

    public virtual SmlRtfHeader? SmlRtfHeader { get; set; }
}

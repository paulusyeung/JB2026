using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class InvoiceHeader
{
    public Guid HeaderId { get; set; }

    public Guid? CustomerId { get; set; }

    public string? BillTo { get; set; }

    public string? ShipTo { get; set; }

    public DateTime InvoiceDate { get; set; }

    public string? InvoiceNumber { get; set; }

    public decimal? InvoiceAmount { get; set; }

    public string? ICNumber { get; set; }

    public DateTime CreatedOn { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public Guid ModifiedBy { get; set; }

    public bool Retired { get; set; }

    public DateTime? RetiredOn { get; set; }

    public Guid? RetiredBy { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
}

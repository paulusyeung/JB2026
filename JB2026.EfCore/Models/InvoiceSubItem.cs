using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class InvoiceSubItem
{
    public Guid SubItemId { get; set; }

    public Guid ItemId { get; set; }

    public int SubLineNumber { get; set; }

    public string? Description { get; set; }

    public decimal? Quantity { get; set; }

    public string? UoM { get; set; }

    public decimal? Price { get; set; }

    public decimal? Amount { get; set; }

    public virtual InvoiceItem Item { get; set; } = null!;
}

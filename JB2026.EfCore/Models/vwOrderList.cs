using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwOrderList
{
    public string? OrderNumber { get; set; }

    public string? CustomerName { get; set; }

    public string? OrderTitle { get; set; }

    public DateTime? OrderedOn { get; set; }

    public string? OrderedBy { get; set; }

    public decimal? InvoiceAmount { get; set; }

    public DateTime? RequiredOn { get; set; }

    public int? JobCount { get; set; }
}

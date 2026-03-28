using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwJobStatCoG
{
    public string? JobNumber { get; set; }

    public string? CustomerName { get; set; }

    public string? OrderTitle { get; set; }

    public string? PurchaseOrder { get; set; }

    public string? SalesRep { get; set; }

    public decimal? Cost { get; set; }

    public decimal? InvoiceAmount { get; set; }

    public string? InvNumber { get; set; }

    public DateTime? InvDate { get; set; }
}

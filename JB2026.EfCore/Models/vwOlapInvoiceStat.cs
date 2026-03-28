using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwOlapInvoiceStat
{
    public string? CustomerName { get; set; }

    public string? InvoiceNumber { get; set; }

    public DateOnly? InvoiceDate { get; set; }

    public decimal? InvoiceAmount { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public string? PurchaseOrder { get; set; }

    public string? ProductCode { get; set; }

    public decimal? Qty { get; set; }

    public string? Unit { get; set; }

    public decimal? Price { get; set; }

    public decimal? Amount { get; set; }
}

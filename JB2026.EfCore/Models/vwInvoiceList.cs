using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwInvoiceList
{
    public Guid? CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public bool? RetiredCustomer { get; set; }

    public Guid HeaderId { get; set; }

    public string? BillTo { get; set; }

    public string? ShipTo { get; set; }

    public string? InvoiceNumber { get; set; }

    public DateTime InvoiceDate { get; set; }

    public decimal? InvoiceAmount { get; set; }

    public string? ICNumber { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public bool RetiredInvoice { get; set; }
}

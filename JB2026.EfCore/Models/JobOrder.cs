using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class JobOrder
{
    public Guid OrderId { get; set; }

    public int OrderType { get; set; }

    public string? OrderNumber { get; set; }

    public int? JobNumber { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerRef { get; set; }

    public string? OrderTitle { get; set; }

    public string? ProductCode { get; set; }

    public string? ProductStyle { get; set; }

    public string? ProductDetails { get; set; }

    public DateTime? OrderedOn { get; set; }

    public string? OrderedBy { get; set; }

    public string? OutputRef { get; set; }

    public string? InvoiceRef { get; set; }

    public decimal? InvoiceAmount { get; set; }

    public decimal? Qty { get; set; }

    public string? QtyText { get; set; }

    public DateTime? RequiredOn { get; set; }

    public DateTime? CompletedOn { get; set; }

    public string? SONumber { get; set; }

    public string? PONumber { get; set; }

    public string? OriginalSONumber { get; set; }

    public string? OriginalPONumber { get; set; }

    public string? PaymentTerms { get; set; }

    public string? Remarks { get; set; }

    public int Status { get; set; }

    public DateTime CreatedOn { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public Guid ModifiedBy { get; set; }

    public bool Retired { get; set; }

    public DateTime? RetiredOn { get; set; }

    public Guid? RetiredBy { get; set; }

    public virtual ICollection<JobAttachment> JobAttachments { get; set; } = new List<JobAttachment>();

    public virtual ICollection<JobPackingOnAir> JobPackingOnAirs { get; set; } = new List<JobPackingOnAir>();

    public virtual ICollection<JobSchedule> JobSchedules { get; set; } = new List<JobSchedule>();

    public virtual ICollection<JobWorkflow> JobWorkflows { get; set; } = new List<JobWorkflow>();
}

using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwJobListWithWorkflow
{
    public Guid OrderId { get; set; }

    public int OrderType { get; set; }

    public string? JobOrderName { get; set; }

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

    public DateTime? RequiredOn { get; set; }

    public string? PaymentTerms { get; set; }

    public string? Remarks { get; set; }

    public int Status { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public bool Retired { get; set; }

    public DateTime? RetiredOn { get; set; }

    public string? RetiredBy { get; set; }

    public int? WorkIndex { get; set; }

    public string? WorkTitle { get; set; }

    public string? WorkInstruction { get; set; }

    public string? WorkNotes { get; set; }

    public string Attachment_Product { get; set; } = null!;

    public decimal InvoiceAmount { get; set; }

    public decimal? Qty { get; set; }

    public string? QtyText { get; set; }

    public DateTime? CompletedOn { get; set; }

    public int? WorkflowFormSeqNumber { get; set; }

    public Guid? FormId { get; set; }

    public Guid? JobWorkflowFormId { get; set; }

    public Guid? WorkflowId { get; set; }

    public string? WorkflowName { get; set; }
}

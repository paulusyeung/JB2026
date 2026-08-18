namespace JB2026.Api.Models;

public sealed class JobOrderPrintDocument
{
    public string OrderNumber { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public string? CustomerRef { get; init; }
    public string? OrderTitle { get; init; }
    public string? ProductCode { get; init; }
    public string? ProductStyle { get; init; }
    public string? ProductDetails { get; init; }
    public string? OrderedBy { get; init; }
    public string? PaymentTerms { get; init; }
    public string? Remarks { get; init; }
    public DateTime? OrderedOn { get; init; }
    public DateTime? ModifiedOn { get; init; }
    public DateTime? RequiredOn { get; init; }
    public string? InvoiceRef { get; init; }
    public decimal? InvoiceAmount { get; init; }
    public decimal? Qty { get; init; }
    public bool NoPicture { get; init; }
    public bool NoProductDetails { get; init; }
    public bool NoRemarks { get; init; }
    public byte[]? ImageBytes { get; init; }
    public IReadOnlyList<JobOrderPrintWorkflow> Workflows { get; init; } = Array.Empty<JobOrderPrintWorkflow>();
}

public sealed class JobOrderPrintWorkflow
{
    public int WorkIndex { get; init; }
    public string? WorkTitle { get; init; }
    public string? WorkInstruction { get; init; }
    public string? WorkNotes { get; init; }
}

namespace JB2026.Api.Models;

public sealed class SmlRtfListResponse
{
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    public required int RowCount { get; init; }

    public required IReadOnlyList<SmlRtfListHeaderResponse> Headers { get; init; }
}

public sealed class SmlRtfListHeaderResponse
{
    public required Guid HeaderId { get; init; }

    public required string RtfFileName { get; init; }

    public required string PurchaseOrder { get; init; }

    public required int RowNumber { get; init; }

    public required string CustomerPO { get; init; }

    public required string OrderedBy { get; init; }

    public required DateTime OrderedOn { get; init; }

    public required string OriginalPO { get; init; }

    public required string SalesOrder { get; init; }

    public required string OriginalSO { get; init; }

    public required int DNCount { get; init; }

    public required int InvoiceCount { get; init; }

    public required string InvoiceNumber { get; init; }

    public required bool IsLabelPrinted { get; init; }

    public required DateTime CreatedOn { get; init; }

    public required string CreatedBy { get; init; }

    public required IReadOnlyList<SmlRtfListItemResponse> Items { get; init; }
}

public sealed class SmlRtfListItemResponse
{
    public required int LineNumber { get; init; }

    public required string ProductCode { get; init; }

    public required string ProductDescription { get; init; }

    public required string Price { get; init; }

    public required string Qty { get; init; }

    public required string Amount { get; init; }
}
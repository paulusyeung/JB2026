namespace JB2026.Api.Models;

public sealed class SmlInvoiceStatsResponse
{
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    public required int RowCount { get; init; }

    public required IReadOnlyList<SmlInvoiceStatsRowResponse> Rows { get; init; }
}

public sealed class SmlInvoiceStatsRowResponse
{
    public required string CustomerName { get; init; }

    public required string InvoiceNumber { get; init; }

    public required DateOnly? InvoiceDate { get; init; }

    public required decimal InvoiceAmount { get; init; }

    public required DateTime? CreatedOn { get; init; }

    public required string CreatedBy { get; init; }

    public required string PurchaseOrder { get; init; }

    public required string ProductCode { get; init; }

    public required decimal Qty { get; init; }

    public required string Unit { get; init; }

    public required decimal Price { get; init; }

    public required decimal Amount { get; init; }

    public required int Year { get; init; }

    public required int Month { get; init; }
}
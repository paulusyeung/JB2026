namespace JB2026.Api.Models;

public sealed class SmlRtfStatsResponse
{
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    public required int RowCount { get; init; }

    public required IReadOnlyList<SmlRtfStatsRowResponse> Rows { get; init; }
}

public sealed class SmlRtfStatsRowResponse
{
    public required string PurchaseOrder { get; init; }

    public required string CustomerPO { get; init; }

    public required DateTime OrderedOn { get; init; }

    public required string OrderedBy { get; init; }

    public required string OriginalPO { get; init; }

    public required string SalesOrder { get; init; }

    public required string OriginalSO { get; init; }

    public required string ProductCode { get; init; }

    public required string Price { get; init; }

    public required string Qty { get; init; }

    public required int Year { get; init; }

    public required int Month { get; init; }

    public required decimal Amount { get; init; }
}
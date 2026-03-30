namespace JB2026.Api.Models;

public sealed class ReportRunResponse
{
    public required string ReportName { get; init; }

    public required DateTimeOffset GeneratedAtUtc { get; init; }

    public required int TotalRows { get; init; }

    public required decimal TotalCostA { get; init; }

    public required IReadOnlyList<QuotationListItemResponse> Rows { get; init; }
}
namespace JB2026.Api.Models;

public sealed class SmlStatsResponse
{
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    public required int RowCount { get; init; }

    public required decimal TotalAmount { get; init; }

    public required IReadOnlyList<SmlMonthlyStatResponse> Monthly { get; init; }

    public required IReadOnlyList<SmlTopCustomerResponse> TopCustomers { get; init; }
}

public sealed class SmlMonthlyStatResponse
{
    public required int Year { get; init; }

    public required int Month { get; init; }

    public required int Count { get; init; }

    public required decimal Amount { get; init; }
}

public sealed class SmlTopCustomerResponse
{
    public required string CustomerName { get; init; }

    public required int Count { get; init; }

    public required decimal Amount { get; init; }
}
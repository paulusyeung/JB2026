namespace JB2026.Api.Models;

public sealed class JobStatsResponse
{
    public required string JobNumber { get; init; }

    public required string CustomerName { get; init; }

    public required string Brand { get; init; }

    public required string PurchaseOrder { get; init; }

    public required string SalesRep { get; init; }

    public required decimal GrossProfit { get; init; }

    public required decimal Cost { get; init; }

    public required decimal InvoiceAmount { get; init; }

    public required string InvNumber { get; init; }

    public DateTime? InvDate { get; init; }

    public int? Year { get; init; }

    public int? Month { get; init; }
}
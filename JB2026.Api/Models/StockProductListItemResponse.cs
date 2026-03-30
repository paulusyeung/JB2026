namespace JB2026.Api.Models;

public sealed class StockProductListItemResponse
{
    public required Guid ProductId { get; init; }

    public required string StockNumber { get; init; }

    public required string ProductCode { get; init; }

    public required string ProductName { get; init; }

    public required int Balance { get; init; }

    public required decimal SellingPrice { get; init; }

    public required decimal COGS { get; init; }

    public required string Remarks { get; init; }
}
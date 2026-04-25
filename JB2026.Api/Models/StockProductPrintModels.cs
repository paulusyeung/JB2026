namespace JB2026.Api.Models;

public sealed class StockProductPrintDocument
{
    public required Guid ProductId { get; init; }

    public required string StockNumber { get; init; }

    public required string ProductCode { get; init; }

    public required string ProductName { get; init; }

    public required string ProductionInfo { get; init; }

    public required string Remarks { get; init; }

    public required int MOQ { get; init; }

    public required int Balance { get; init; }

    public required IReadOnlyList<StockProductPrintMovementRow> Movements { get; init; }
}

public sealed class StockProductPrintMovementRow
{
    public required int RowNumber { get; init; }

    public required DateTime InOutDate { get; init; }

    public required string Reference { get; init; }

    public required int Qty { get; init; }

    public required int RunningBalance { get; init; }

    public required DateTime ModifiedOn { get; init; }

    public required string ModifiedBy { get; init; }
}
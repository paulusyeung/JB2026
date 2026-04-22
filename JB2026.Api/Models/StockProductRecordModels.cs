namespace JB2026.Api.Models;

public sealed class StockProductRecordResponse
{
    public required Guid ProductId { get; init; }

    public required string CustomerCode { get; init; }

    public required string CategoryCode { get; init; }

    public required string SequenceNumber { get; init; }

    public required string StockNumber { get; init; }

    public required string ProductCode { get; init; }

    public required string ProductName { get; init; }

    public required string ProductionInfo { get; init; }

    public required string Remarks { get; init; }

    public required decimal SellingPrice { get; init; }

    public required decimal COGS { get; init; }

    public required int Balance { get; init; }

    public required DateTime CreatedOn { get; init; }

    public required string CreatedBy { get; init; }

    public required DateTime ModifiedOn { get; init; }

    public required string ModifiedBy { get; init; }
}

public sealed class StockProductRecordUpsertRequest
{
    public required string CustomerCode { get; init; }

    public required string CategoryCode { get; init; }

    public required string SequenceNumber { get; init; }

    public required string ProductCode { get; init; }

    public required string ProductName { get; init; }

    public string? ProductionInfo { get; init; }

    public string? Remarks { get; init; }

    public decimal SellingPrice { get; init; }

    public decimal COGS { get; init; }
}

public sealed class StockProductCodeValidationResponse
{
    public required bool IsUnique { get; init; }
}

public sealed class StockProductNextNumberResponse
{
    public required string CustomerCode { get; init; }

    public required string CategoryCode { get; init; }

    public required string SequenceNumber { get; init; }

    public required string StockNumber { get; init; }
}

public sealed class StockMovementHistoryItemResponse
{
    public required Guid InOutId { get; init; }

    public required DateTime InOutDate { get; init; }

    public required string Reference { get; init; }

    public required int Qty { get; init; }

    public required int RunningBalance { get; init; }

    public required DateTime ModifiedOn { get; init; }

    public required string ModifiedBy { get; init; }
}
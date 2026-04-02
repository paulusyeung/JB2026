namespace JB2026.Api.Models;

public sealed class QuotationListItemResponse
{
    public required Guid HeaderId { get; init; }

    public required string MachineType { get; init; }

    public required int QuoteNumber { get; init; }

    public required int QuoteNumberIndex { get; init; }

    public required string QuoteNumberIndexPair { get; init; }

    public required DateTime QuotedOn { get; init; }

    public required string QuotedBy { get; init; }

    public DateTime? ApprovedOn { get; init; }

    public string? ApprovedBy { get; init; }

    public required string PrintTitle { get; init; }

    public required string CustomerName { get; init; }

    public required string PrintsSize { get; init; }

    public required string PrintsColor { get; init; }

    public required decimal PrintsQty { get; init; }

    public required string MaterialName { get; init; }

    public required decimal MaterialCost { get; init; }

    public required decimal TotalCostA { get; init; }

    public required decimal UnitCostA { get; init; }

    public required int Status { get; init; }

    public required DateTime CreatedOn { get; init; }

    public required string CreatedBy { get; init; }

    public required DateTime ModifiedOn { get; init; }

    public required string ModifiedBy { get; init; }
}

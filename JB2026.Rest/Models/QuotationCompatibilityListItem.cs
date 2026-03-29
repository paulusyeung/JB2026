namespace JB2026.Rest.Models;

public sealed class QuotationCompatibilityListItem
{
    public Guid HeaderId { get; init; }
    public string MachineType { get; init; } = string.Empty;
    public int QuoteNumber { get; init; }
    public int QuoteNumberIndex { get; init; }
    public string QuoteNumberIndexPair { get; init; } = string.Empty;
    public DateTime QuotedOn { get; init; }
    public string QuotedBy { get; init; } = string.Empty;
    public DateTime? ApprovedOn { get; init; }
    public string? ApprovedBy { get; init; }
    public string PrintTitle { get; init; } = string.Empty;
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string PrintsSize { get; init; } = string.Empty;
    public string PrintsColor { get; init; } = string.Empty;
    public decimal PrintsQty { get; init; }
    public string PaperSheetSize { get; init; } = string.Empty;
    public string MaterialName { get; init; } = string.Empty;
    public decimal MaterialCost { get; init; }
    public string PaperSheetSizeAlias { get; init; } = string.Empty;
    public string PaperSizeFormat { get; init; } = string.Empty;
    public int PrintsPerSheet { get; init; }
    public int PrintsPerPage { get; init; }
    public string PrintPerPageEx { get; init; } = string.Empty;
    public decimal PageWidth { get; init; }
    public decimal PageHeight { get; init; }
    public decimal TotalCostA { get; init; }
    public decimal TotalCostB { get; init; }
    public decimal TotalCostC { get; init; }
    public decimal TotalCostD { get; init; }
    public decimal UnitCostA { get; init; }
    public decimal UnitCostB { get; init; }
    public decimal UnitCostC { get; init; }
    public decimal UnitCostD { get; init; }
    public int Status { get; init; }
    public Guid ModifiedBy { get; init; }
    public DateTime ModifiedOn { get; init; }
    public Guid CreatedBy { get; init; }
    public DateTime CreatedOn { get; init; }
    public bool Retired { get; init; }
    public Guid? RetiredBy { get; init; }
    public DateTime? RetiredOn { get; init; }
}
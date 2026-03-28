using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwQtHeaderList
{
    public Guid HeaderId { get; set; }

    public int MachineType { get; set; }

    public int QuoteNumber { get; set; }

    public int QuoteNumberIndex { get; set; }

    public DateTime QuotedOn { get; set; }

    public string? QuotedBy { get; set; }

    public DateTime? ApprovedOn { get; set; }

    public string? ApprovedBy { get; set; }

    public string? PrintTitle { get; set; }

    public Guid? CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public string? PrintsSize { get; set; }

    public string? PrintsColor { get; set; }

    public string? PrintsQty { get; set; }

    public string? PaperSheetSize { get; set; }

    public string? MaterialName { get; set; }

    public string? MaterialCost { get; set; }

    public string? PaperSheetSizeAlias { get; set; }

    public int? PaperSizeFormat { get; set; }

    public int? PrintsPerSheet { get; set; }

    public int? PrintsPerPage { get; set; }

    public string? PrintPerPageEx { get; set; }

    public string? PageWidth { get; set; }

    public string? PageHeight { get; set; }

    public decimal? TotalCostA { get; set; }

    public decimal? TotalCostB { get; set; }

    public decimal? TotalCostC { get; set; }

    public decimal? TotalCostD { get; set; }

    public decimal? UnitCostA { get; set; }

    public decimal? UnitCostB { get; set; }

    public decimal? UnitCostC { get; set; }

    public decimal? UnitCostD { get; set; }

    public int Status { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public bool Retired { get; set; }

    public DateTime? RetiredOn { get; set; }

    public string? RetiredBy { get; set; }
}

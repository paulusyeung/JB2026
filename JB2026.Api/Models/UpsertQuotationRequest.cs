using System.ComponentModel.DataAnnotations;

namespace JB2026.Api.Models;

public sealed class UpsertQuotationRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int QuoteNumber { get; init; }

    [Required]
    [Range(1, int.MaxValue)]
    public int QuoteNumberIndex { get; init; }

    [Required]
    [StringLength(160)]
    public string CustomerName { get; init; } = string.Empty;

    [StringLength(160)]
    public string PrintTitle { get; init; } = string.Empty;

    [Required]
    public DateTime QuotedOn { get; init; }

    [Required]
    [StringLength(64)]
    public string QuotedBy { get; init; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal TotalCostA { get; init; }

    [Range(0, double.MaxValue)]
    public decimal UnitCostA { get; init; }

    [Range(0, int.MaxValue)]
    public int Status { get; init; }
}

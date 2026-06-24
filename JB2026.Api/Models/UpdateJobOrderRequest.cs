using System.ComponentModel.DataAnnotations;

namespace JB2026.Api.Models;

public sealed class UpdateJobOrderRequest
{
    [Required]
    [StringLength(128)]
    public string CustomerName { get; init; } = string.Empty;

    [StringLength(64)]
    public string CustomerRef { get; init; } = string.Empty;

    [Required]
    [StringLength(160)]
    public string OrderTitle { get; init; } = string.Empty;

    [Required]
    public DateTime RequiredOn { get; init; }

    [Range(0.01, 1000000)]
    public decimal Qty { get; init; }

    [Required]
    [StringLength(64)]
    public string PaymentTerms { get; init; } = string.Empty;

    [StringLength(512)]
    public string Remarks { get; init; } = string.Empty;

    public string? ProductDetails { get; init; }

    [Range(0, 99)]
    public int Status { get; init; }

    [Range(0, 3)]
    public int OrderType { get; init; }

    [StringLength(32)]
    public string? SONumber { get; init; }

    [StringLength(32)]
    public string? OriginalSONumber { get; init; }

    [StringLength(256)]
    public string? ProductStyle { get; init; }

    [StringLength(128)]
    public string? ProductCode { get; init; }

    [StringLength(128)]
    public string? OutputRef { get; init; }

    [StringLength(128)]
    public string? InvoiceRef { get; init; }

    public decimal? InvoiceAmount { get; init; }

    public Dictionary<string, string>? WorkflowAttributes { get; init; }
}

using System.ComponentModel.DataAnnotations;

namespace JB2026.Api.Models;

public sealed class CreateJobOrderRequest
{
    [Required]
    [StringLength(32)]
    public string OrderNumber { get; init; } = string.Empty;

    [Required]
    [StringLength(16)]
    public string JobNumber { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string CustomerName { get; init; } = string.Empty;

    [StringLength(64)]
    public string CustomerRef { get; init; } = string.Empty;

    [Required]
    [StringLength(160)]
    public string OrderTitle { get; init; } = string.Empty;

    [Required]
    public DateTime OrderedOn { get; init; }

    [Required]
    public DateTime RequiredOn { get; init; }

    [Range(0.01, 1000000)]
    public decimal Qty { get; init; }

    [Required]
    [StringLength(64)]
    public string PaymentTerms { get; init; } = string.Empty;

    [StringLength(512)]
    public string Remarks { get; init; } = string.Empty;

    [Range(0, 99)]
    public int Status { get; init; }

    [Range(0, 3)]
    public int OrderType { get; init; }
}

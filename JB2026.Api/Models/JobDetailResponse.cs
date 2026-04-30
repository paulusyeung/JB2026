namespace JB2026.Api.Models;

public sealed class JobDetailResponse
{
    public required Guid OrderId { get; init; }

    public required string OrderNumber { get; init; }

    public required string CustomerName { get; init; }

    public required string CustomerRef { get; init; }

    public required string OrderTitle { get; init; }

    public required string OrderedBy { get; init; }

    public required DateTime OrderedOn { get; init; }

    public required DateTime RequiredOn { get; init; }

    public required int Status { get; init; }

    public required decimal Qty { get; init; }

    public required string PaymentTerms { get; init; }

    public required string Remarks { get; init; }

    public required string ProductDetails { get; init; }

    public required string[] StyleTitles { get; init; }

    public required IReadOnlyList<JobAttachmentResponse> Attachments { get; init; }
}

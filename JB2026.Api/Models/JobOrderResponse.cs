namespace JB2026.Api.Models;

public sealed class JobOrderResponse
{
    public required Guid OrderId { get; init; }

    public required int OrderType { get; init; }

    public required string OrderNumber { get; init; }

    public required string JobNumber { get; init; }

    public required string CustomerName { get; init; }

    public required string CustomerRef { get; init; }

    public required string OrderTitle { get; init; }

    public required string ProductCode { get; init; }

    public required string ProductStyle { get; init; }

    public required string OutputRef { get; init; }

    public required string InvoiceRef { get; init; }

    public required decimal InvoiceAmount { get; init; }

    public required int AttachmentProductCount { get; init; }

    public required int AttachmentCustomerCount { get; init; }

    public required string OrderedBy { get; init; }

    public required DateTime OrderedOn { get; init; }

    public required DateTime RequiredOn { get; init; }

    public DateTime? CompletedOn { get; init; }

    public required decimal Qty { get; init; }

    public required string PaymentTerms { get; init; }

    public required string Remarks { get; init; }

    public required int Status { get; init; }

    public required string CreatedBy { get; init; }

    public required DateTime CreatedOn { get; init; }

    public string? ModifiedBy { get; init; }

    public DateTime? ModifiedOn { get; init; }
}

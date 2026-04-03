namespace JB2026.Api.Models;

public sealed class JobSchedulePendingItemResponse
{
    public required Guid OrderId { get; init; }

    public required int OrderType { get; init; }

    public required string OrderNumber { get; init; }

    public required string CustomerName { get; init; }

    public required string OrderTitle { get; init; }

    public required int Status { get; init; }

    public DateTime? OrderedOn { get; init; }

    public DateTime? RequiredOn { get; init; }

    public int UrgencyLevel { get; init; }

    public int? Step1Status { get; init; }

    public int? Step2Status { get; init; }

    public int? Step3Status { get; init; }
}

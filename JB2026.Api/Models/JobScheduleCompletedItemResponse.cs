namespace JB2026.Api.Models;

public sealed class JobScheduleCompletedItemResponse
{
    public required Guid OrderId { get; init; }

    public required int OrderType { get; init; }

    public required string OrderNumber { get; init; }

    public required string CustomerName { get; init; }

    public required string OrderTitle { get; init; }

    public required int Status { get; init; }

    public required string MachineNumber { get; init; }

    public DateTime? OrderedOn { get; init; }

    public DateTime? RequiredOn { get; init; }

    public DateTime? ScheduledOn { get; init; }

    public DateTime? CompletedOn { get; init; }
}

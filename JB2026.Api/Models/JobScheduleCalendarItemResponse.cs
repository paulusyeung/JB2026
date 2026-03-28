namespace JB2026.Api.Models;

public sealed class JobScheduleCalendarItemResponse
{
    public required Guid ScheduleId { get; init; }

    public required Guid OrderId { get; init; }

    public required string Title { get; init; }

    public required DateTime StartOn { get; init; }

    public DateTime? EndOn { get; init; }

    public int? Status { get; init; }

    public int? Priority { get; init; }

    public string? MachineNumber { get; init; }
}

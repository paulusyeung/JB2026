namespace JB2026.Api.Models;

public sealed class RescheduleCompletedSchedulesRequest
{
    public IReadOnlyList<Guid> OrderIds { get; init; } = Array.Empty<Guid>();
}

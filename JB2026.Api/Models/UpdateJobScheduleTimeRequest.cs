namespace JB2026.Api.Models;

public sealed class UpdateJobScheduleTimeRequest
{
    public DateTime? StartOn { get; init; }

    public DateTime? EndOn { get; init; }
}

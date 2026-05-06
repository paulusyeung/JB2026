namespace JB2026.Api.Models;

public sealed class PendingUrgencyUpdateResponse
{
    public required Guid OrderId { get; init; }

    /// <summary>
    /// Normalized urgency level after update. -1 = neutral, 2 = yellow, 4 = red.
    /// </summary>
    public required int UrgencyLevel { get; init; }
}

namespace JB2026.Api.Models;

public sealed class UpdatePendingUrgencyRequest
{
    /// <summary>
    /// Target urgency color. Accepted values: "red" or "yellow".
    /// Clicking the currently active color toggles urgency back to neutral (-1).
    /// </summary>
    public required string TargetColor { get; init; }
}

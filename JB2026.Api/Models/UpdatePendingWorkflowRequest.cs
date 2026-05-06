namespace JB2026.Api.Models;

public sealed class UpdatePendingWorkflowRequest
{
    /// <summary>
    /// Zero-based workflow step index (0 = step 1, 1 = step 2, 2 = step 3).
    /// </summary>
    public required int StepIndex { get; init; }

    /// <summary>
    /// Target workflow status code (0 = red, 1 = yellow, 2 = green, 3 = blue/info).
    /// </summary>
    public required int TargetStatus { get; init; }
}

namespace JB2026.Api.Models;

public sealed class PendingWorkflowUpdateResponse
{
    public required Guid OrderId { get; init; }

    public int? Step1Status { get; init; }

    public int? Step2Status { get; init; }

    public int? Step3Status { get; init; }
}

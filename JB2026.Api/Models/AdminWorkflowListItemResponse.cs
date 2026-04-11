namespace JB2026.Api.Models;

public sealed class AdminWorkflowListItemResponse
{
    public required Guid WorkflowId { get; init; }

    public required string WorkflowName { get; init; }

    public required string WorkTitle { get; init; }

    public required string WorkInstruction { get; init; }
}

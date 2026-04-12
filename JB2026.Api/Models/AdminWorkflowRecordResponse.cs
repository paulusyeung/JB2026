namespace JB2026.Api.Models;

public sealed class AdminWorkflowRecordResponse
{
    public Guid WorkflowId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public string WorkTitle { get; set; } = string.Empty;
    public string WorkInstruction { get; set; } = string.Empty;
}

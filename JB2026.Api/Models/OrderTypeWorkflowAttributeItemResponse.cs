namespace JB2026.Api.Models;

public sealed class OrderTypeWorkflowAttributeItemResponse
{
    public int WorkIndex { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public IReadOnlyList<string> Options { get; set; } = [];
}

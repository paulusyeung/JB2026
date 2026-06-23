namespace JB2026.Api.Models;

public sealed class OrderTypeWorkflowAttributeResponse
{
    public IReadOnlyList<OrderTypeWorkflowAttributeItemResponse> WorkflowAttributes { get; set; } = [];
}

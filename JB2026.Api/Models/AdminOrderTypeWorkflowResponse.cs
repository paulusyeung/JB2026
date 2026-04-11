namespace JB2026.Api.Models;

public sealed class AdminOrderTypeWorkflowResponse
{
    public IReadOnlyList<AdminOrderTypeWorkflowItemResponse> AvailableWorkflows { get; set; } = [];
    public IReadOnlyList<AdminOrderTypeWorkflowItemResponse> SelectedWorkflows { get; set; } = [];
}

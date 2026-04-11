using System.ComponentModel.DataAnnotations;

namespace JB2026.Api.Models;

public sealed class UpdateAdminOrderTypeWorkflowsRequest
{
    [Range(0, 3)]
    public int OrderType { get; set; }

    public IReadOnlyList<Guid> WorkflowIds { get; set; } = [];
}

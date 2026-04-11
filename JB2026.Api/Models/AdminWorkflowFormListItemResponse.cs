namespace JB2026.Api.Models;

public sealed class AdminWorkflowFormListItemResponse
{
    public Guid FormId { get; set; }
    public string FormName { get; set; } = string.Empty;
    public string FormNameChs { get; set; } = string.Empty;
    public string FormNameCht { get; set; } = string.Empty;
}

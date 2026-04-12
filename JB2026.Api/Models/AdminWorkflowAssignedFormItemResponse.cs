namespace JB2026.Api.Models;

public sealed class AdminWorkflowAssignedFormItemResponse
{
    public Guid WorkflowFormId { get; set; }
    public Guid FormId { get; set; }
    public int SeqNumber { get; set; }
    public string FormName { get; set; } = string.Empty;
    public string FormNameChs { get; set; } = string.Empty;
    public string FormNameCht { get; set; } = string.Empty;
    public string? MetadataXml { get; set; }
}

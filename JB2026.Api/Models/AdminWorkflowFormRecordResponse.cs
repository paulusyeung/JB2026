namespace JB2026.Api.Models;

public sealed class AdminWorkflowFormRecordResponse
{
    public Guid FormId { get; set; }
    public int FormObjectEnum { get; set; }
    public string FormName { get; set; } = string.Empty;
    public string FormNameChs { get; set; } = string.Empty;
    public string FormNameCht { get; set; } = string.Empty;
    public string? MetadataXml { get; set; }
}

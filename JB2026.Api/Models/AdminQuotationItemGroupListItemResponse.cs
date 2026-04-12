namespace JB2026.Api.Models;

public sealed class AdminQuotationItemGroupListItemResponse
{
    public Guid ItemGroupId { get; init; }
    public string Zone { get; init; } = string.Empty;
    public string GroupNameEn { get; init; } = string.Empty;
    public string GroupNameCht { get; init; } = string.Empty;
    public string GroupNameChs { get; init; } = string.Empty;
    public DateTime CreatedOn { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime ModifiedOn { get; init; }
    public string ModifiedBy { get; init; } = string.Empty;
}
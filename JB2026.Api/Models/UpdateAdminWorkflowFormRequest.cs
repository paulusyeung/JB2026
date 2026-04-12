using System.ComponentModel.DataAnnotations;

namespace JB2026.Api.Models;

public sealed class UpdateAdminWorkflowFormRequest
{
    [Required]
    [MaxLength(10)]
    public string FormName { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string FormNameChs { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string FormNameCht { get; set; } = string.Empty;

    public string? MetadataXml { get; set; }
}

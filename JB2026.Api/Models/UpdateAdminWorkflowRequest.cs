using System.ComponentModel.DataAnnotations;

namespace JB2026.Api.Models;

public sealed class UpdateAdminWorkflowRequest
{
    [Required]
    [MaxLength(64)]
    public string WorkflowName { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string WorkTitle { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string WorkInstruction { get; set; } = string.Empty;
}

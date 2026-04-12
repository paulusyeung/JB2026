using System.ComponentModel.DataAnnotations;

namespace JB2026.Api.Models;

public sealed class UpdateAdminWorkflowFormsRequest
{
    [Required]
    public List<Guid> FormIds { get; set; } = [];
}

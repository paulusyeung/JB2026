using System.ComponentModel.DataAnnotations;

namespace JB2026.Api.Models;

public sealed class UpsertUserPreferenceRequest
{
    [Required]
    public string Metadata { get; init; } = string.Empty;
}

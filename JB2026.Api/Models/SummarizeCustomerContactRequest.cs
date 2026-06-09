using System.ComponentModel.DataAnnotations;

namespace JB2026.Api.Models;

public sealed class SummarizeCustomerContactRequest
{
    [Required]
    [StringLength(10240, ErrorMessage = "Input text must not exceed 10KB.")]
    public string RawContactText { get; init; } = string.Empty;

    public bool PersistResult { get; init; }

    public bool OverwriteExistingSummary { get; init; }
}

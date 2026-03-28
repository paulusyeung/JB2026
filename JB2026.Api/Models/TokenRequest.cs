using System.ComponentModel.DataAnnotations;

namespace JB2026.Api.Models;

public sealed class TokenRequest
{
    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

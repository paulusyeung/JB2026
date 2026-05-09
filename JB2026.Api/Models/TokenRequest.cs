using System.ComponentModel.DataAnnotations;

namespace JB2026.Api.Models;

public sealed class TokenRequest
{
    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// If true, a refresh token will be issued alongside the access token.
    /// Defaults to false for backward compatibility.
    /// </summary>
    public bool KeepMeSignedIn { get; init; } = false;
}

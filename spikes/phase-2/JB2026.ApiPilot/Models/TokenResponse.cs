namespace JB2026.ApiPilot.Models;

public sealed class TokenResponse
{
    public required string AccessToken { get; init; }

    public required DateTime ExpiresAtUtc { get; init; }
}
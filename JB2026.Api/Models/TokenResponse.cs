namespace JB2026.Api.Models;

public sealed class TokenResponse
{
    public required string AccessToken { get; init; }

    public required DateTime ExpiresAtUtc { get; init; }

    public required string TokenType { get; init; }

    public required UserProfileResponse User { get; init; }
}

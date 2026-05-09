namespace JB2026.Api.Models;

public sealed class TokenResponse
{
    public required string AccessToken { get; init; }

    public required DateTime ExpiresAtUtc { get; init; }

    public required string TokenType { get; init; }

    public required UserProfileResponse User { get; init; }

    /// <summary>
    /// Optional refresh token issued when KeepMeSignedIn is true.
    /// When present, can be used to obtain a new access token via the refresh endpoint.
    /// </summary>
    public string? RefreshToken { get; init; }
}

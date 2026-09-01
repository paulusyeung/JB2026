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

    /// <summary>
    /// When true, the user must complete 2FA verification before receiving a full access token.
    /// The TwoFactorToken contains a short-lived token for the second step.
    /// </summary>
    public bool Requires2fa { get; init; }

    /// <summary>
    /// Short-lived token (5 minutes) for completing 2FA verification.
    /// Only present when Requires2fa is true.
    /// </summary>
    public string? TwoFactorToken { get; init; }
}

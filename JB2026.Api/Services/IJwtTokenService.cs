namespace JB2026.Api.Services;

public interface IJwtTokenService
{
    /// <summary>
    /// Creates a JWT access token for the specified user.
    /// </summary>
    /// <param name="user">The user to create a token for.</param>
    /// <param name="keepMeSignedIn">If true, indicates that the caller will manage a refresh token. Does not affect token lifetime.</param>
    /// <returns>A tuple containing the token string and its expiration time.</returns>
    (string Token, DateTime ExpiresAtUtc) CreateToken(LegacyIdentityUser user, bool keepMeSignedIn = false);
}

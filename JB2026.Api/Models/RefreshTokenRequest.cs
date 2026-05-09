namespace JB2026.Api.Models
{
    /// <summary>
    /// Request contract for the /api/v2/auth/refresh endpoint.
    /// </summary>
    public sealed class RefreshTokenRequest
    {
        /// <summary>
        /// The refresh token to exchange for a new access token.
        /// </summary>
        public required string RefreshToken { get; init; }
    }
}

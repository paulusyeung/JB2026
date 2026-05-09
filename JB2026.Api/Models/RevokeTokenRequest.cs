namespace JB2026.Api.Models
{
    /// <summary>
    /// Request contract for the /api/v2/auth/revoke endpoint.
    /// </summary>
    public sealed class RevokeTokenRequest
    {
        /// <summary>
        /// The refresh token to revoke/invalidate.
        /// </summary>
        public required string RefreshToken { get; init; }
    }
}

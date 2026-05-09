namespace JB2026.Api.Models
{
    /// <summary>
    /// Represents a refresh token record for token exchange and rotation.
    /// </summary>
    public class RefreshTokenRecord
    {
        /// <summary>
        /// The opaque refresh token string (Base64-encoded).
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// The user ID associated with this refresh token.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// The expiration time of the refresh token in UTC.
        /// </summary>
        public DateTime ExpiresAtUtc { get; set; }

        /// <summary>
        /// Indicates whether this refresh token has been used.
        /// Once used, it is marked as invalid to detect token theft.
        /// </summary>
        public bool IsUsed { get; set; }

        /// <summary>
        /// The creation time of the refresh token in UTC.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }
    }
}

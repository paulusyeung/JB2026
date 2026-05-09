namespace JB2026.Api.Services
{
    /// <summary>
    /// Service for managing refresh tokens.
    /// Provides methods to create, validate, and revoke refresh tokens.
    /// </summary>
    public interface IRefreshTokenService
    {
        /// <summary>
        /// Creates a new refresh token for the specified user.
        /// </summary>
        /// <param name="userId">The user ID for which the token is being created.</param>
        /// <param name="expiryDays">The number of days until the token expires.</param>
        /// <returns>A task that returns the newly created refresh token string.</returns>
        Task<string> CreateAsync(string userId, int expiryDays);

        /// <summary>
        /// Validates a refresh token and returns the associated user ID if valid.
        /// If the token has been used (indicating theft), revokes all tokens for that user.
        /// </summary>
        /// <param name="refreshToken">The refresh token to validate.</param>
        /// <returns>A task that returns the user ID if valid, or null if invalid/expired.</returns>
        Task<string?> ValidateAsync(string refreshToken);

        /// <summary>
        /// Atomically validates and consumes a refresh token in a single operation.
        /// Removes the token from the store if valid, preventing race conditions.
        /// Returns the associated user ID if valid, or null if invalid/expired/already consumed.
        /// </summary>
        /// <param name="refreshToken">The refresh token to validate and consume.</param>
        /// <returns>A task that returns the user ID if valid, or null if invalid/expired/already consumed.</returns>
        Task<string?> ValidateAndConsumeAsync(string refreshToken);

        /// <summary>
        /// Revokes (invalidates) a specific refresh token.
        /// Does nothing if the token is unknown (idempotent).
        /// </summary>
        /// <param name="refreshToken">The refresh token to revoke.</param>
        /// <returns>A task that completes when the token is revoked.</returns>
        Task RevokeAsync(string refreshToken);

        /// <summary>
        /// Revokes all refresh tokens for a specific user.
        /// Used when token theft is detected (reused token).
        /// </summary>
        /// <param name="userId">The user ID for which all tokens should be revoked.</param>
        /// <returns>A task that completes when all tokens are revoked.</returns>
        Task RevokeAllForUserAsync(string userId);
    }
}

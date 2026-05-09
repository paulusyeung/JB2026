using System.Collections.Concurrent;

namespace JB2026.Api.Services
{
    /// <summary>
    /// In-memory implementation of the refresh token service.
    /// Uses a ConcurrentDictionary for thread-safe storage.
    /// Note: Tokens are lost on server restart. For production multi-server deployments, upgrade to database-backed storage.
    /// </summary>
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly ConcurrentDictionary<string, Models.RefreshTokenRecord> _tokenStore =
            new ConcurrentDictionary<string, Models.RefreshTokenRecord>();

        /// <summary>
        /// Creates a new refresh token for the specified user.
        /// </summary>
        public async Task<string> CreateAsync(string userId, int expiryDays)
        {
            var token = GenerateTokenString();
            var record = new Models.RefreshTokenRecord
            {
                Token = token,
                UserId = userId,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(expiryDays),
                IsUsed = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            _tokenStore.TryAdd(token, record);
            return await Task.FromResult(token);
        }

        /// <summary>
        /// Validates a refresh token and returns the associated user ID if valid.
        /// If the token has been used (indicating theft), revokes all tokens for that user.
        /// </summary>
        public async Task<string?> ValidateAsync(string refreshToken)
        {
            if (!_tokenStore.TryGetValue(refreshToken, out var record))
            {
                return null; // Token not found
            }

            // Check expiration
            if (record.ExpiresAtUtc < DateTime.UtcNow)
            {
                return null; // Token expired
            }

            // Check if already used (token theft detection)
            if (record.IsUsed)
            {
                // Revoke all tokens for this user
                await RevokeAllForUserAsync(record.UserId);
                return null; // This token is invalid (already used)
            }

            return await Task.FromResult(record.UserId);
        }

        /// <summary>
        /// Atomically validates and consumes a refresh token.
        /// Uses TryRemove for atomic validation and consumption.
        /// </summary>
        public async Task<string?> ValidateAndConsumeAsync(string refreshToken)
        {
            // Atomically remove the token from the store
            if (!_tokenStore.TryRemove(refreshToken, out var record))
            {
                return null; // Token not found (already consumed or never existed)
            }

            // Check expiration after successful remove
            if (record.ExpiresAtUtc < DateTime.UtcNow)
            {
                return null; // Token expired
            }

            // Check if already marked as used
            if (record.IsUsed)
            {
                return null; // Token was already used
            }

            return await Task.FromResult(record.UserId);
        }

        /// <summary>
        /// Revokes (invalidates) a specific refresh token.
        /// </summary>
        public async Task RevokeAsync(string refreshToken)
        {
            _tokenStore.TryRemove(refreshToken, out _);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Revokes all refresh tokens for a specific user.
        /// </summary>
        public async Task RevokeAllForUserAsync(string userId)
        {
            var tokensToRemove = _tokenStore
                .Where(kvp => kvp.Value.UserId == userId)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var token in tokensToRemove)
            {
                _tokenStore.TryRemove(token, out _);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Generates a random opaque token string (64 bytes, Base64-encoded).
        /// </summary>
        private static string GenerateTokenString()
        {
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var tokenData = new byte[64];
                rng.GetBytes(tokenData);
                return Convert.ToBase64String(tokenData);
            }
        }
    }
}

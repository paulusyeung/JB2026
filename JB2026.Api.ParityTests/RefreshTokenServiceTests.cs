using JB2026.Api.Models;
using JB2026.Api.Services;
using Xunit;

namespace JB2026.Api.Tests
{
    public class RefreshTokenServiceTests
    {
        [Fact]
        public async Task CreateAsync_CreatesAndReturnsToken()
        {
            // Arrange
            var service = new RefreshTokenService();
            var userId = "test-user-123";
            var expiryDays = 30;

            // Act
            var token = await service.CreateAsync(userId, expiryDays);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.True(token.Length > 0);
        }

        [Fact]
        public async Task ValidateAsync_ValidTokenReturnsUserId()
        {
            // Arrange
            var service = new RefreshTokenService();
            var userId = "test-user-456";
            var token = await service.CreateAsync(userId, 30);

            // Act
            var result = await service.ValidateAsync(token);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result);
        }

        [Fact]
        public async Task ValidateAsync_InvalidTokenReturnsNull()
        {
            // Arrange
            var service = new RefreshTokenService();
            var invalidToken = "invalid-token-that-does-not-exist";

            // Act
            var result = await service.ValidateAsync(invalidToken);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateAsync_ExpiredTokenReturnsNull()
        {
            // Arrange
            var service = new RefreshTokenService();
            var userId = "test-user-789";
            // Create a token that expires in -1 days (already expired)
            var token = await service.CreateAsync(userId, -1);

            // Act
            var result = await service.ValidateAsync(token);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateAsync_UsedTokenReturnsNullAndRevokesAllUserTokens()
        {
            // Arrange
            var service = new RefreshTokenService();
            var userId = "test-user-theft";
            var token1 = await service.CreateAsync(userId, 30);
            var token2 = await service.CreateAsync(userId, 30);

            // First validation marks token1 as used
            var result1 = await service.ValidateAsync(token1);
            Assert.NotNull(result1);

            // Simulate second validation of the same token (theft detected)
            var result2 = await service.ValidateAsync(token1);
            Assert.Null(result2); // Should be null because it's used

            // Token2 should also be revoked because all tokens for the user should be revoked
            var result3 = await service.ValidateAsync(token2);
            Assert.Null(result3);
        }

        [Fact]
        public async Task RevokeAsync_InvalidatesToken()
        {
            // Arrange
            var service = new RefreshTokenService();
            var userId = "test-user-revoke";
            var token = await service.CreateAsync(userId, 30);

            // Act
            await service.RevokeAsync(token);
            var result = await service.ValidateAsync(token);

            // Assert
            Assert.Null(result); // Token should be invalid after revocation
        }

        [Fact]
        public async Task RevokeAsync_NonExistentToken_IsIdempotent()
        {
            // Arrange
            var service = new RefreshTokenService();
            var nonExistentToken = "non-existent-token";

            // Act & Assert - should not throw
            await service.RevokeAsync(nonExistentToken);
        }

        [Fact]
        public async Task RevokeAllForUserAsync_RevokesAllUserTokens()
        {
            // Arrange
            var service = new RefreshTokenService();
            var userId = "test-user-revoke-all";
            var token1 = await service.CreateAsync(userId, 30);
            var token2 = await service.CreateAsync(userId, 30);

            // Act
            await service.RevokeAllForUserAsync(userId);
            var result1 = await service.ValidateAsync(token1);
            var result2 = await service.ValidateAsync(token2);

            // Assert
            Assert.Null(result1);
            Assert.Null(result2);
        }

        [Fact]
        public async Task CreateAsync_GeneratesUniqueTokens()
        {
            // Arrange
            var service = new RefreshTokenService();
            var userId = "test-user-unique";

            // Act
            var token1 = await service.CreateAsync(userId, 30);
            var token2 = await service.CreateAsync(userId, 30);

            // Assert
            Assert.NotEqual(token1, token2);
        }
    }
}

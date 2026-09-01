using JB2026.Api.Options;
using Microsoft.Extensions.Options;

namespace JB2026.Api.Services;

/// <summary>
/// Identity service for config-based users only.
/// Config-based users do not have database rows, so 2FA is not supported.
/// </summary>
public sealed class ConfiguredLegacyIdentityService : ILegacyIdentityService
{
    private readonly IReadOnlyList<LegacyIdentityUser> _users;
    private readonly IReadOnlyDictionary<string, LegacyIdentityUser> _usersByUsername;
    private readonly IReadOnlyDictionary<Guid, LegacyIdentityUser> _usersById;

    public ConfiguredLegacyIdentityService(IOptions<LegacyIdentityOptions> options)
    {
        var users = options.Value.Users
            .Select(user => new LegacyIdentityUser
            {
                UserId = user.UserId,
                Username = user.Username,
                Password = user.Password,
                DisplayName = user.DisplayName,
                Role = user.Role
            })
            .ToList();

        _users = users;
        _usersByUsername = users.ToDictionary(user => user.Username, StringComparer.OrdinalIgnoreCase);
        _usersById = users.ToDictionary(user => user.UserId);
    }

    public LegacyIdentityUser? ValidateCredentials(string username, string password)
    {
        return _usersByUsername.TryGetValue(username, out var user) && user.Password == password
            ? user
            : null;
    }

    public LegacyIdentityUser? FindByUsername(string username)
    {
        return _usersByUsername.TryGetValue(username, out var user) ? user : null;
    }

    public LegacyIdentityUser? FindByUserId(Guid userId)
    {
        return _usersById.TryGetValue(userId, out var user) ? user : null;
    }

    public IReadOnlyList<LegacyIdentityUser> GetUsers()
    {
        return _users;
    }

    // 2FA is not supported for config-based users (no database row for MetadataXml)
    public Task<bool> GetTwoFactorStatusAsync(Guid userId) => Task.FromResult(false);

    public Task EnableTwoFactorAsync(Guid userId, string secret, string recoveryCodes) =>
        throw new NotSupportedException("2FA is not supported for config-based users.");

    public Task DisableTwoFactorAsync(Guid userId) =>
        throw new NotSupportedException("2FA is not supported for config-based users.");

    public Task<bool> ValidateTwoFactorCodeAsync(Guid userId, string code) => Task.FromResult(false);

    public Task<bool> UseRecoveryCodeAsync(Guid userId, string code) => Task.FromResult(false);

    public Task<string?> GetUserInfoMetadataAsync(Guid userId) => Task.FromResult<string?>(null);
}

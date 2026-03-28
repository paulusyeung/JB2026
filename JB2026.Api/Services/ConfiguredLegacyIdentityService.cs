using JB2026.Api.Options;
using Microsoft.Extensions.Options;

namespace JB2026.Api.Services;

public sealed class ConfiguredLegacyIdentityService : ILegacyIdentityService
{
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
}

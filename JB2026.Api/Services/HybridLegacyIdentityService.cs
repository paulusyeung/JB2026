using System.Globalization;
using JB2026.Api.Options;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JB2026.Api.Services;

public sealed class HybridLegacyIdentityService : ILegacyIdentityService
{
    private readonly IReadOnlyList<LegacyIdentityUser> _configuredUsers;
    private readonly IReadOnlyDictionary<string, LegacyIdentityUser> _configuredByUsername;
    private readonly IReadOnlyDictionary<Guid, LegacyIdentityUser> _configuredByUserId;
    private readonly JB5LegacyReadContext? _readContext;

    public HybridLegacyIdentityService(IOptions<LegacyIdentityOptions> options, IServiceProvider serviceProvider)
    {
        var configuredUsers = options.Value.Users
            .Select(user => new LegacyIdentityUser
            {
                UserId = user.UserId,
                Username = user.Username,
                Password = user.Password,
                DisplayName = user.DisplayName,
                Role = user.Role
            })
            .ToList();

        _configuredUsers = configuredUsers;
        _configuredByUsername = configuredUsers.ToDictionary(user => user.Username, StringComparer.OrdinalIgnoreCase);
        _configuredByUserId = configuredUsers.ToDictionary(user => user.UserId);
        _readContext = serviceProvider.GetService<JB5LegacyReadContext>();
    }

    public LegacyIdentityUser? ValidateCredentials(string username, string password)
    {
        if (_configuredByUsername.TryGetValue(username, out var configured) && configured.Password == password)
        {
            return configured;
        }

        if (_readContext is null)
        {
            return null;
        }

        var normalizedUsername = username.Trim();
        var dbUser = _readContext.vwUserList_Actives
            .AsNoTracking()
            .FirstOrDefault(x => (x.UserName ?? string.Empty) == normalizedUsername && (x.UserPassword ?? string.Empty) == password);

        return dbUser is null ? null : MapDbUser(dbUser);
    }

    public LegacyIdentityUser? FindByUsername(string username)
    {
        if (_configuredByUsername.TryGetValue(username, out var configured))
        {
            return configured;
        }

        if (_readContext is null)
        {
            return null;
        }

        var normalizedUsername = username.Trim();
        var dbUser = _readContext.vwUserList_Actives
            .AsNoTracking()
            .FirstOrDefault(x => (x.UserName ?? string.Empty) == normalizedUsername);

        return dbUser is null ? null : MapDbUser(dbUser);
    }

    public LegacyIdentityUser? FindByUserId(Guid userId)
    {
        if (_configuredByUserId.TryGetValue(userId, out var configured))
        {
            return configured;
        }

        if (_readContext is null)
        {
            return null;
        }

        var dbUser = _readContext.vwUserList_Actives
            .AsNoTracking()
            .FirstOrDefault(x => x.UserId == userId);

        return dbUser is null ? null : MapDbUser(dbUser);
    }

    public IReadOnlyList<LegacyIdentityUser> GetUsers()
    {
        if (_readContext is null)
        {
            return _configuredUsers;
        }

        var dbUsers = _readContext.vwUserList_Actives
            .AsNoTracking()
            .Select(MapDbUser)
            .ToList();

        return _configuredUsers
            .Concat(dbUsers)
            .GroupBy(user => user.UserId)
            .Select(group => group.First())
            .ToList();
    }

    private static LegacyIdentityUser MapDbUser(vwUserList_Active user)
    {
        var username = (user.UserName ?? string.Empty).Trim();
        return new LegacyIdentityUser
        {
            UserId = user.UserId,
            Username = username,
            Password = user.UserPassword ?? string.Empty,
            DisplayName = string.IsNullOrWhiteSpace(user.UserAlias) ? username : user.UserAlias,
            Role = user.UserRole.ToString(CultureInfo.InvariantCulture)
        };
    }
}
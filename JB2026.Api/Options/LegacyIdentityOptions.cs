namespace JB2026.Api.Options;

/// <summary>
/// Configuration for legacy identity users defined in appsettings.
/// Config-based users do not have a database row, so 2FA is not available for them.
/// 2FA is only supported for users stored in the UserInfo database table.
/// </summary>
public sealed class LegacyIdentityOptions
{
    public const string SectionName = "LegacyIdentity";

    public List<LegacyIdentityUserOptions> Users { get; init; } = [];
}

public sealed class LegacyIdentityUserOptions
{
    public Guid UserId { get; init; }

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;
}

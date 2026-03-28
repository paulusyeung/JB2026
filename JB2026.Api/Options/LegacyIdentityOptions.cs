namespace JB2026.Api.Options;

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

namespace JB2026.Api.Services;

public sealed class LegacyIdentityUser
{
    public required Guid UserId { get; init; }

    public required string Username { get; init; }

    public required string Password { get; init; }

    public required string DisplayName { get; init; }

    public required string Role { get; init; }

    public bool TwoFactorEnabled { get; init; }

    public string? TwoFactorSecret { get; init; }

    public string? TwoFactorRecoveryCodes { get; init; }
}

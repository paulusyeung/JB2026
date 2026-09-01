namespace JB2026.Api.Services;

public interface ILegacyIdentityService
{
    LegacyIdentityUser? ValidateCredentials(string username, string password);

    LegacyIdentityUser? FindByUsername(string username);

    LegacyIdentityUser? FindByUserId(Guid userId);

    IReadOnlyList<LegacyIdentityUser> GetUsers();

    Task<bool> GetTwoFactorStatusAsync(Guid userId);

    Task EnableTwoFactorAsync(Guid userId, string secret, string recoveryCodes);

    Task DisableTwoFactorAsync(Guid userId);

    Task<bool> ValidateTwoFactorCodeAsync(Guid userId, string code);

    Task<bool> UseRecoveryCodeAsync(Guid userId, string code);

    Task<string?> GetUserInfoMetadataAsync(Guid userId);
}

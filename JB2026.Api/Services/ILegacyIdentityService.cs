namespace JB2026.Api.Services;

public interface ILegacyIdentityService
{
    LegacyIdentityUser? ValidateCredentials(string username, string password);

    LegacyIdentityUser? FindByUsername(string username);

    LegacyIdentityUser? FindByUserId(Guid userId);
}

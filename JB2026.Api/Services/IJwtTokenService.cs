namespace JB2026.Api.Services;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(LegacyIdentityUser user);
}

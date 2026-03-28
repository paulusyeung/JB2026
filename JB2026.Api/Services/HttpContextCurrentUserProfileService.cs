using System.Security.Claims;
using JB2026.Api.Models;
using Microsoft.AspNetCore.Http;

namespace JB2026.Api.Services;

public sealed class HttpContextCurrentUserProfileService : ICurrentUserProfileService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILegacyIdentityService _legacyIdentityService;

    public HttpContextCurrentUserProfileService(
        IHttpContextAccessor httpContextAccessor,
        ILegacyIdentityService legacyIdentityService)
    {
        _httpContextAccessor = httpContextAccessor;
        _legacyIdentityService = legacyIdentityService;
    }

    public UserProfileResponse? GetCurrentUser()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = principal.FindFirstValue(ClaimTypes.Name);

        LegacyIdentityUser? user = null;

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            user = _legacyIdentityService.FindByUserId(userId);
        }

        user ??= !string.IsNullOrWhiteSpace(username)
            ? _legacyIdentityService.FindByUsername(username)
            : null;

        if (user is null)
        {
            return null;
        }

        return new UserProfileResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Role = user.Role
        };
    }
}

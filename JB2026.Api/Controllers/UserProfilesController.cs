using JB2026.Api.Models;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/user-profiles")]
public sealed class UserProfilesController : ControllerBase
{
    private readonly ICurrentUserProfileService _currentUserProfileService;
    private readonly ILegacyIdentityService _legacyIdentityService;
    private readonly ILogger<UserProfilesController> _logger;

    public UserProfilesController(
        ICurrentUserProfileService currentUserProfileService,
        ILegacyIdentityService legacyIdentityService,
        ILogger<UserProfilesController> logger)
    {
        _currentUserProfileService = currentUserProfileService;
        _legacyIdentityService = legacyIdentityService;
        _logger = logger;
    }

    [HttpGet("me")]
    public ActionResult<UserProfileResponse> GetCurrentUser()
    {
        var user = _currentUserProfileService.GetCurrentUser();
        if (user is null)
        {
            _logger.LogWarning("Authenticated request could not be resolved to a configured user profile.");
            return NotFound(new ProblemDetails
            {
                Title = "User profile not found",
                Detail = "The authenticated user could not be mapped to a configured identity profile.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(user);
    }

    [HttpGet("{username}")]
    public ActionResult<UserProfileResponse> GetByUsername(string username)
    {
        var user = _legacyIdentityService.FindByUsername(username);
        if (user is null)
        {
            _logger.LogInformation("User profile lookup failed for username {Username}", username);
            return NotFound(new ProblemDetails
            {
                Title = "User profile not found",
                Detail = $"No configured identity profile exists for username '{username}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(new UserProfileResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Role = user.Role
        });
    }
}

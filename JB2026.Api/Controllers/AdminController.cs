using JB2026.Api.Models;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly ILegacyIdentityService _legacyIdentityService;

    public AdminController(ILegacyIdentityService legacyIdentityService)
    {
        _legacyIdentityService = legacyIdentityService;
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminUserResponse>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<AdminUserResponse>> GetUsers()
    {
        var users = _legacyIdentityService
            .GetUsers()
            .Select(user => new AdminUserResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Role = user.Role,
            })
            .OrderBy(user => user.Username, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Ok(users);
    }
}
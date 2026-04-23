using System.Security.Claims;
using JB2026.Api.Models;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/user-preferences")]
public sealed class UserPreferencesController : ControllerBase
{
    private readonly JB5LegacyWriteContext _writeContext;

    public UserPreferencesController(JB5LegacyWriteContext writeContext)
    {
        _writeContext = writeContext;
    }

    [HttpGet("{objectType:int}/{objectId:guid}")]
    [ProducesResponseType(typeof(UserPreferenceMetadataResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserPreferenceMetadataResponse>> Get(
        int objectType,
        Guid objectId,
        CancellationToken cancellationToken = default)
    {
        var userId = await GetCurrentUserIdAsync(cancellationToken);

        var metadata = await _writeContext.UserPreferences
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.ObjectType == objectType && item.ObjectId == objectId)
            .Select(item => item.MetadataXml)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new UserPreferenceMetadataResponse
        {
            Metadata = metadata
        });
    }

    [HttpPut("{objectType:int}/{objectId:guid}")]
    [ProducesResponseType(typeof(UserPreferenceMetadataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserPreferenceMetadataResponse>> Put(
        int objectType,
        Guid objectId,
        [FromBody] UpsertUserPreferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = await GetCurrentUserIdAsync(cancellationToken);

        var existing = await _writeContext.UserPreferences
            .FirstOrDefaultAsync(item => item.UserId == userId && item.ObjectType == objectType && item.ObjectId == objectId, cancellationToken);

        if (existing is null)
        {
            existing = new UserPreference
            {
                PreferenceId = Guid.NewGuid(),
                UserId = userId,
                ObjectType = objectType,
                ObjectId = objectId,
                MetadataXml = request.Metadata
            };

            _writeContext.UserPreferences.Add(existing);
        }
        else
        {
            existing.MetadataXml = request.Metadata;
        }

        await _writeContext.SaveChangesAsync(cancellationToken);

        return Ok(new UserPreferenceMetadataResponse
        {
            Metadata = existing.MetadataXml
        });
    }

    private async Task<Guid> GetCurrentUserIdAsync(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (Guid.TryParse(userIdClaim, out var claimedUserId))
        {
            var exists = await _writeContext.Users
                .AsNoTracking()
                .AnyAsync(user => user.UserId == claimedUserId, cancellationToken);

            if (exists)
            {
                return claimedUserId;
            }
        }

        var loginName = User.FindFirstValue(ClaimTypes.Name);
        if (!string.IsNullOrWhiteSpace(loginName))
        {
            var userId = await _writeContext.Users
                .AsNoTracking()
                .Where(user => user.LoginName == loginName && !user.Retired)
                .Select(user => user.UserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (userId != Guid.Empty)
            {
                return userId;
            }
        }

        throw new BadHttpRequestException("Authenticated user identifier is missing or cannot be mapped.", StatusCodes.Status401Unauthorized);
    }
}

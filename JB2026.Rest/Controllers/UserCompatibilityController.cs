using System.Security.Claims;
using System.Text.Json;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Rest.Controllers;

[ApiController]
[Authorize]
public sealed class UserCompatibilityController : ControllerBase
{
    private readonly ILegacyIdentityService _legacyIdentityService;

    public UserCompatibilityController(ILegacyIdentityService legacyIdentityService)
    {
        _legacyIdentityService = legacyIdentityService;
    }

    [HttpGet("api/User")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = ResolveCurrentUserId();
        if (userId is null)
        {
            return BadRequest("Invalid User Sid");
        }

        if (TryGetContexts(out var readContext, out _) && readContext is not null)
        {
            var dbUser = await ResolveRequestedDbUserAsync(readContext, userId.Value, cancellationToken);
            if (dbUser is not null)
            {
                return Ok(dbUser);
            }
        }

        var user = _legacyIdentityService.FindByUserId(userId.Value);
        if (user is null)
        {
            return BadRequest($"Invalid User Sid: {userId}");
        }

        return Ok(ToLegacyUserResponse(user));
    }

    [HttpGet("api/User/{userkey}")]
    public async Task<IActionResult> Get(string userkey, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userkey, out var userId))
        {
            return NotFound();
        }

        if (TryGetContexts(out var readContext, out _) && readContext is not null)
        {
            var dbUser = await ResolveRequestedDbUserAsync(readContext, userId, cancellationToken);
            if (dbUser is not null)
            {
                return Ok(dbUser);
            }
        }

        var user = _legacyIdentityService.FindByUserId(userId);
        return user is null ? NotFound() : Ok(ToLegacyUserResponse(user));
    }

    [HttpGet("api/User/Notification/{deviceid}")]
    public async Task<IActionResult> GetNotification(string deviceid, CancellationToken cancellationToken)
    {
        if (!TryGetContexts(out var readContext, out _))
        {
            return MissingDatabaseResponse();
        }

        var currentUser = await ResolveCurrentDbUserAsync(readContext, cancellationToken);
        if (currentUser is null)
        {
            return NotFound();
        }

        var item = await readContext.UserNotifications
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserId == currentUser.UserId && x.DeviceId == deviceid,
                cancellationToken);

        if (item is null || string.IsNullOrWhiteSpace(item.MetadataXml))
        {
            return NotFound();
        }

        try
        {
            using var document = JsonDocument.Parse(item.MetadataXml);
            return Ok(document.RootElement.Clone());
        }
        catch (JsonException)
        {
            return Ok(item.MetadataXml);
        }
    }

    [HttpPost("api/User/Notification/{id}")]
    public async Task<IActionResult> PostNotification(string id, CancellationToken cancellationToken)
    {
        _ = id;
        if (!TryGetContexts(out var readContext, out var writeContext))
        {
            return MissingDatabaseResponse();
        }

        var currentUser = await ResolveCurrentDbUserAsync(readContext, cancellationToken);
        if (currentUser is null)
        {
            return NotFound();
        }

        string rawJson;
        using (var reader = new StreamReader(Request.Body))
        {
            rawJson = await reader.ReadToEndAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return BadRequest("Request body is required.");
        }

        using var jsonDoc = JsonDocument.Parse(rawJson);
        if (!TryParseNotificationPayload(jsonDoc.RootElement, out var deviceId, out var platform, out var options))
        {
            return BadRequest("Invalid notification payload.");
        }

        foreach (var option in options)
        {
            var notifyType = MapNotifyType(option.Key);
            if (notifyType == 0)
            {
                continue;
            }

            var existing = await writeContext.UserNotifications
                .SingleOrDefaultAsync(
                    x => x.UserId == currentUser.UserId && x.DeviceId == deviceId && x.NotifyType == notifyType,
                    cancellationToken);

            if (option.Value)
            {
                if (existing is null)
                {
                    writeContext.UserNotifications.Add(new UserNotification
                    {
                        NotifyId = Guid.NewGuid(),
                        UserId = currentUser.UserId,
                        DeviceId = deviceId,
                        NotifyType = notifyType,
                        Platform = platform,
                        MetadataXml = rawJson
                    });
                }
                else
                {
                    existing.Platform = platform;
                    existing.MetadataXml = rawJson;
                }
            }
            else if (existing is not null)
            {
                writeContext.UserNotifications.Remove(existing);
            }
        }

        await writeContext.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    private Guid? ResolveCurrentUserId()
    {
        if (TryParseGuidClaim(ClaimTypes.NameIdentifier, out var id))
        {
            return id;
        }

        if (TryParseGuidClaim(ClaimTypes.Name, out id))
        {
            return id;
        }

        if (TryParseGuidClaim("sub", out id))
        {
            return id;
        }

        return null;
    }

    private bool TryParseGuidClaim(string claimType, out Guid id)
    {
        id = Guid.Empty;
        var value = User.FindFirstValue(claimType);
        return !string.IsNullOrWhiteSpace(value) && Guid.TryParse(value, out id);
    }

    private static object ToLegacyUserResponse(LegacyIdentityUser user)
    {
        var roleName = string.IsNullOrWhiteSpace(user.Role) ? "Unknown" : user.Role;
        return new
        {
            UserId = user.UserId,
            UserType = 0,
            UserSid = user.UserId,
            LoginName = user.Username,
            LoginPassword = user.Password,
            Alias = user.DisplayName,
            Status = 1,
            CreatedOn = DateTime.UnixEpoch,
            CreatedBy = user.UserId,
            ModifiedOn = DateTime.UnixEpoch,
            ModifiedBy = user.UserId,
            Retired = false,
            RetiredOn = (DateTime?)null,
            RetiredBy = (Guid?)null,
            UserRole = MapUserRole(roleName),
            UserRoleName = roleName,
            UserAuth = (object?)null,
            UserNotification = (object?)null,
            UserPreference = (object?)null
        };
    }

    private static int MapUserRole(string roleName)
    {
        return roleName.ToLowerInvariant() switch
        {
            "admin" => 4,
            "manager" => 3,
            "staff" => 2,
            "client" => 1,
            "customer" => 1,
            _ => 0
        };
    }

    private bool TryGetContexts(out JB5LegacyReadContext readContext, out JB5LegacyWriteContext writeContext)
    {
        var services = HttpContext.RequestServices;
        readContext = services.GetService<JB5LegacyReadContext>()!;
        writeContext = services.GetService<JB5LegacyWriteContext>()!;

        return readContext is not null && writeContext is not null;
    }

    private async Task<User?> ResolveCurrentDbUserAsync(JB5LegacyReadContext readContext, CancellationToken cancellationToken)
    {
        var userId = ResolveCurrentUserId();
        if (userId is null)
        {
            return null;
        }

        return await readContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserSid == userId.Value || x.UserId == userId.Value, cancellationToken);
    }

    private static async Task<object?> ResolveRequestedDbUserAsync(
        JB5LegacyReadContext readContext,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await readContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserSid == userId || x.UserId == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var userInfo = await readContext.UserInfos
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == user.UserId, cancellationToken);

        var role = userInfo?.UserRole ?? 0;
        return new
        {
            user.UserId,
            user.UserType,
            user.UserSid,
            user.LoginName,
            user.LoginPassword,
            user.Alias,
            user.Status,
            user.CreatedOn,
            user.CreatedBy,
            user.ModifiedOn,
            user.ModifiedBy,
            user.Retired,
            user.RetiredOn,
            user.RetiredBy,
            UserRole = role,
            UserRoleName = MapUserRoleName(role),
            UserAuth = (object?)null,
            UserNotification = (object?)null,
            UserPreference = (object?)null
        };
    }

    private static bool TryParseNotificationPayload(
        JsonElement root,
        out string deviceId,
        out int platform,
        out IReadOnlyDictionary<string, bool> options)
    {
        deviceId = string.Empty;
        platform = 0;
        var map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        if (!TryGetPropertyCaseInsensitive(root, "DeviceInfo", out var deviceInfo)
            || !TryGetPropertyCaseInsensitive(deviceInfo, "Id", out var deviceIdProp)
            || deviceIdProp.ValueKind != JsonValueKind.String)
        {
            options = map;
            return false;
        }

        deviceId = deviceIdProp.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            options = map;
            return false;
        }

        if (TryGetPropertyCaseInsensitive(deviceInfo, "Platform", out var platformProp) && platformProp.TryGetInt32(out var parsedPlatform))
        {
            platform = parsedPlatform;
        }

        if (!TryGetPropertyCaseInsensitive(root, "Options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Object)
        {
            options = map;
            return false;
        }

        foreach (var property in optionsElement.EnumerateObject())
        {
            if (property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                map[property.Name] = property.Value.GetBoolean();
            }
        }

        options = map;
        return map.Count > 0;
    }

    private static int MapNotifyType(string optionName)
    {
        return optionName.ToLowerInvariant() switch
        {
            "everyone" => 1,
            "staffonly" => 2,
            "onorder" => 10,
            "onscheduled" => 11,
            "onready_paper" => 12,
            "onreadypaper" => 12,
            "onready_plate" => 13,
            "onreadyplate" => 13,
            "onready_final" => 14,
            "onreadyfinal" => 14,
            "onfileagent" => 30,
            _ => 0
        };
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement source, string propertyName, out JsonElement value)
    {
        if (source.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in source.EnumerateObject())
            {
                if (property.NameEquals(propertyName) || property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string MapUserRoleName(int role)
    {
        return role switch
        {
            4 => "Admin",
            3 => "Manager",
            2 => "Staff",
            1 => "Client",
            _ => "Unknown"
        };
    }

    private static ObjectResult MissingDatabaseResponse()
    {
        return new ObjectResult(new ProblemDetails
        {
            Title = "Not implemented",
            Detail = "This endpoint requires the Primary database connection.",
            Status = StatusCodes.Status501NotImplemented
        })
        {
            StatusCode = StatusCodes.Status501NotImplemented
        };
    }
}

using System.Security.Claims;
using System.Text.Json;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using JB2026.Rest.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Rest.Controllers;

[ApiController]
[Authorize]
public sealed class FcmCompatibilityController : ControllerBase
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;
    private readonly IWebhookDispatcherService _webhookDispatcher;

    public FcmCompatibilityController(
        JB5LegacyReadContext readContext,
        JB5LegacyWriteContext writeContext,
        IWebhookDispatcherService webhookDispatcher)
    {
        _readContext = readContext;
        _writeContext = writeContext;
        _webhookDispatcher = webhookDispatcher;
    }

    [HttpPost("api/FCM/Register/{id}")]
    public async Task<IActionResult> PostRegister(string id, CancellationToken cancellationToken)
    {
        _ = id;

        var user = await ResolveCurrentUserAsync(cancellationToken);
        if (user is null)
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
        if (!TryParseAuthPayload(jsonDoc.RootElement, out var deviceId, out var platform))
        {
            return BadRequest("Invalid FCM registration payload.");
        }

        var auth = await _writeContext.UserAuths
            .SingleOrDefaultAsync(x => x.DeviceId == deviceId, cancellationToken);

        if (auth is null)
        {
            _writeContext.UserAuths.Add(new UserAuth
            {
                AuthId = Guid.NewGuid(),
                UserId = user.UserId,
                DeviceId = deviceId,
                AuthType = 3,
                Platform = platform,
                MetadataXml = rawJson
            });
        }
        else
        {
            auth.UserId = user.UserId;
            auth.AuthType = 3;
            auth.Platform = platform;
            auth.MetadataXml = rawJson;

            var staleNotifications = _writeContext.UserNotifications.Where(x => x.DeviceId == deviceId && x.UserId != user.UserId);
            _writeContext.UserNotifications.RemoveRange(staleNotifications);
        }

        await _writeContext.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    [HttpPost("FCM/BroadcastMessage/{topic}/{msg}")]
    [AllowAnonymous]
    public async Task<IActionResult> PostBroadcastMessage(string topic, string msg, CancellationToken cancellationToken)
    {
        await AddHistoryAsync(topic, msg, "everyone", string.Empty, cancellationToken);
        return Ok();
    }

    [HttpPost("FCM/SendMessage/OnOrder/{id:guid}")]
    [AllowAnonymous]
    public Task<IActionResult> PostSendOnOrder(Guid id, CancellationToken cancellationToken)
    {
        return AddEventHistoryAsync("OnOrder", id, cancellationToken);
    }

    [HttpPost("FCM/SendMessage/OnScheduled/{id:guid}")]
    [AllowAnonymous]
    public Task<IActionResult> PostSendOnScheduled(Guid id, CancellationToken cancellationToken)
    {
        return AddEventHistoryAsync("OnScheduled", id, cancellationToken);
    }

    [HttpPost("FCM/SendMessage/OnReadyPaper/{id:guid}")]
    [AllowAnonymous]
    public Task<IActionResult> PostSendOnReadyPaper(Guid id, CancellationToken cancellationToken)
    {
        return AddEventHistoryAsync("OnReadyPaper", id, cancellationToken);
    }

    [HttpPost("FCM/SendMessage/OnReadyPlate/{id:guid}")]
    [AllowAnonymous]
    public Task<IActionResult> PostSendOnReadyPlate(Guid id, CancellationToken cancellationToken)
    {
        return AddEventHistoryAsync("OnReadyPlate", id, cancellationToken);
    }

    [HttpPost("FCM/SendMessage/OnReadyFinal/{id:guid}")]
    [AllowAnonymous]
    public Task<IActionResult> PostSendOnReadyFinal(Guid id, CancellationToken cancellationToken)
    {
        return AddEventHistoryAsync("OnReadyFinal", id, cancellationToken);
    }

    [HttpPost("api/FCM/SendMessage/{id}")]
    public async Task<IActionResult> PostSendMessage(string id, CancellationToken cancellationToken)
    {
        var payload = await ReadRequestBodySafeAsync(cancellationToken);
        await AddHistoryAsync("direct", payload, id, string.Empty, cancellationToken);
        return Ok();
    }

    [HttpPost("api/FCM/BroadcastMessage/{id}")]
    public async Task<IActionResult> PostBroadcastMessageById(string id, CancellationToken cancellationToken)
    {
        var payload = await ReadRequestBodySafeAsync(cancellationToken);
        await AddHistoryAsync("broadcast", payload, "everyone", id, cancellationToken);
        return Ok();
    }

    private async Task<IActionResult> AddEventHistoryAsync(string eventName, Guid id, CancellationToken cancellationToken)
    {
        await AddHistoryAsync(eventName, id.ToString(), "staffonly", string.Empty, cancellationToken);
        return Ok();
    }

    private async Task AddHistoryAsync(
        string topic,
        string message,
        string recipients,
        string userIds,
        CancellationToken cancellationToken)
    {
        var createdOn = DateTime.Now;
        _writeContext.FCMHistories.Add(new FCMHistory
        {
            FCMHistoryId = Guid.NewGuid(),
            MessageTitle = topic,
            MessageBody = message,
            DeliveredOn = createdOn,
            Topic = topic,
            RecipientList = recipients,
            UserIdList = userIds
        });

        await _writeContext.SaveChangesAsync(cancellationToken);

        await _webhookDispatcher.EnqueueEventAsync(topic, new
        {
            Topic = topic,
            Message = message,
            Recipients = recipients,
            UserIds = userIds,
            CreatedOn = createdOn
        }, cancellationToken);
    }

    private async Task<User?> ResolveCurrentUserAsync(CancellationToken cancellationToken)
    {
        var sid = ResolveCurrentSid();
        if (sid is null)
        {
            return null;
        }

        return await _readContext.Users
            .SingleOrDefaultAsync(x => x.UserSid == sid.Value || x.UserId == sid.Value, cancellationToken);
    }

    private Guid? ResolveCurrentSid()
    {
        var candidate = User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(candidate, out var sid) ? sid : null;
    }

    private static bool TryParseAuthPayload(JsonElement root, out string deviceId, out int platform)
    {
        deviceId = string.Empty;
        platform = 0;

        if (!TryGetPropertyCaseInsensitive(root, "DeviceInfo", out var deviceInfo)
            || !TryGetPropertyCaseInsensitive(deviceInfo, "Id", out var deviceIdProp)
            || deviceIdProp.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        deviceId = deviceIdProp.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return false;
        }

        if (TryGetPropertyCaseInsensitive(deviceInfo, "Platform", out var platformProp) && platformProp.TryGetInt32(out var parsedPlatform))
        {
            platform = parsedPlatform;
        }

        return true;
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

    private async Task<string> ReadRequestBodySafeAsync(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(payload) ? string.Empty : payload;
    }
}

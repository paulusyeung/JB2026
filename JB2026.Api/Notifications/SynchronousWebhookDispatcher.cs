using System.Text;
using System.Text.Json;
using JB2026.EfCore.Data;
using JB2026.EfCore.Notifications;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Notifications;

/// <summary>
/// Synchronous webhook dispatcher for the production (JB2026.Api) host.
/// Reads active subscriptions, POSTs JSON to matching URLs, and tolerates
/// failures — mirroring the Rest host dispatcher's inline fallback path.
/// </summary>
public sealed class SynchronousWebhookDispatcher : IWebhookEventDispatcher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JB5LegacyWriteContext _writeContext;
    private readonly ILogger<SynchronousWebhookDispatcher> _logger;

    public SynchronousWebhookDispatcher(
        IHttpClientFactory httpClientFactory,
        JB5LegacyWriteContext writeContext,
        ILogger<SynchronousWebhookDispatcher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _writeContext = writeContext;
        _logger = logger;
    }

    public async Task EnqueueEventAsync(string eventType, object payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            return;
        }

        var subscriptions = await _writeContext.WebhookSubscriptions
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        var matched = subscriptions
            .Where(x => IsEventSubscribed(x.EventTypes, eventType))
            .Select(x => x.Url)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (matched.Count == 0)
        {
            return;
        }

        var jsonPayload = JsonSerializer.Serialize(payload);

        foreach (var url in matched)
        {
            await DispatchAsync(url!, jsonPayload, cancellationToken);
        }
    }

    public async Task DispatchAsync(string url, string jsonPayload, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            _logger.LogWarning("Skipping webhook dispatch because URL is invalid: {Url}", url);
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            var client = _httpClientFactory.CreateClient(nameof(SynchronousWebhookDispatcher));
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Webhook dispatch returned non-success status code {StatusCode} for {Url}",
                    (int)response.StatusCode,
                    url);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook dispatch failed for {Url}", url);
        }
    }

    private static bool IsEventSubscribed(string? eventTypes, string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventTypes))
        {
            return false;
        }

        return eventTypes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(x => x.Equals(eventType, StringComparison.OrdinalIgnoreCase));
    }
}
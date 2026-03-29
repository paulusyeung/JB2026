using System.Text;
using System.Text.Json;
using Hangfire;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Rest.Helpers;

public sealed class WebhookDispatcherService : IWebhookDispatcherService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JB5LegacyReadContext _readContext;
    private readonly ILogger<WebhookDispatcherService> _logger;
    private readonly IBackgroundJobClient? _backgroundJobs;

    public WebhookDispatcherService(
        IHttpClientFactory httpClientFactory,
        JB5LegacyReadContext readContext,
        ILogger<WebhookDispatcherService> logger,
        IBackgroundJobClient? backgroundJobs = null)
    {
        _httpClientFactory = httpClientFactory;
        _readContext = readContext;
        _logger = logger;
        _backgroundJobs = backgroundJobs;
    }

    public async Task EnqueueEventAsync(string eventType, object payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            return;
        }

        var subscriptions = await _readContext.WebhookSubscriptions
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0)
        {
            return;
        }

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
            if (_backgroundJobs is not null)
            {
                _backgroundJobs.Enqueue<WebhookDispatcherService>(
                    svc => svc.DispatchAsync(url!, jsonPayload, CancellationToken.None));
                continue;
            }

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

            var client = _httpClientFactory.CreateClient(nameof(WebhookDispatcherService));
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
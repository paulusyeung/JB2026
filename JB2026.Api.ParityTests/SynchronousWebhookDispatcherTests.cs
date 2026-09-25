using System.Net;
using JB2026.Api.Notifications;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JB2026.Api.ParityTests;

/// <summary>
/// Unit tests for <see cref="SynchronousWebhookDispatcher"/>: subscription
/// matching and tolerant synchronous delivery of webhook events.
/// </summary>
public sealed class SynchronousWebhookDispatcherTests
{
    private const string HookUrl = "https://example.com/hook";

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static JB5LegacyWriteContext CreateContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<JB5LegacyWriteContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString("N"))
            .Options;
        return new JB5LegacyWriteContext(options);
    }

    private static SynchronousWebhookDispatcher CreateDispatcher(
        JB5LegacyWriteContext context,
        StubHttpMessageHandler handler)
    {
        var factory = new StubHttpClientFactory(handler);
        return new SynchronousWebhookDispatcher(factory, context, NullLogger<SynchronousWebhookDispatcher>.Instance);
    }

    private static WebhookSubscription CreateSubscription(string eventTypes, bool isActive = true, string url = HookUrl)
    {
        return new WebhookSubscription
        {
            Url = url,
            EventTypes = eventTypes,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
        };
    }

    // -----------------------------------------------------------------------
    // Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EnqueueEventAsync_PostsJsonToMatchingSubscription()
    {
        using var context = CreateContext();
        context.WebhookSubscriptions.Add(CreateSubscription("OnJobCreated,OnJobCompleted"));
        await context.SaveChangesAsync();

        var handler = new StubHttpMessageHandler();
        var dispatcher = CreateDispatcher(context, handler);

        await dispatcher.EnqueueEventAsync("OnJobCreated", new { EventType = "OnJobCreated", OrderId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") }, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(HookUrl, request.Url);
        Assert.Contains("OnJobCreated", request.Body, StringComparison.Ordinal);
        Assert.Contains("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnqueueEventAsync_IgnoresNonMatchingSubscription()
    {
        using var context = CreateContext();
        context.WebhookSubscriptions.Add(CreateSubscription("OnReadyPlate"));
        await context.SaveChangesAsync();

        var handler = new StubHttpMessageHandler();
        var dispatcher = CreateDispatcher(context, handler);

        await dispatcher.EnqueueEventAsync("OnJobCreated", new { EventType = "OnJobCreated" }, CancellationToken.None);

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task EnqueueEventAsync_IgnoresInactiveSubscription()
    {
        using var context = CreateContext();
        context.WebhookSubscriptions.Add(CreateSubscription("OnJobCreated", isActive: false));
        await context.SaveChangesAsync();

        var handler = new StubHttpMessageHandler();
        var dispatcher = CreateDispatcher(context, handler);

        await dispatcher.EnqueueEventAsync("OnJobCreated", new { EventType = "OnJobCreated" }, CancellationToken.None);

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task EnqueueEventAsync_ToleratesNonSuccessStatusCode()
    {
        using var context = CreateContext();
        context.WebhookSubscriptions.Add(CreateSubscription("OnJobInvoiced"));
        await context.SaveChangesAsync();

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var dispatcher = CreateDispatcher(context, handler);

        await dispatcher.EnqueueEventAsync("OnJobInvoiced", new { EventType = "OnJobInvoiced" }, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.NotNull(request);
    }

    [Fact]
    public async Task EnqueueEventAsync_SubscriptionReadFailure_DoesNotThrow()
    {
        // Regression: the subscription read used to sit outside the try/catch, so
        // a failure there (e.g. "Invalid object name 'dbo.WebhookSubscriptions'")
        // propagated into the business operation that emitted the event.
        var context = CreateContext();
        var handler = new StubHttpMessageHandler();
        var dispatcher = CreateDispatcher(context, handler);

        // Disposing the context makes the WebhookSubscriptions read throw, which
        // is the closest in-memory stand-in for a missing table.
        context.Dispose();

        await dispatcher.EnqueueEventAsync("OnJobScheduled", new { EventType = "OnJobScheduled" }, CancellationToken.None);

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task DispatchAsync_InvalidUrl_DoesNotSend()
    {
        using var context = CreateContext();
        var handler = new StubHttpMessageHandler();
        var dispatcher = CreateDispatcher(context, handler);

        await dispatcher.DispatchAsync("not-a-valid-url", "{}", CancellationToken.None);

        Assert.Empty(handler.Requests);
    }

    private sealed class StubHttpClientFactory(StubHttpMessageHandler handler) : IHttpClientFactory
    {
        private readonly HttpClient _client = new(handler);

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public List<CapturedRequest> Requests { get; } = [];

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage>? responder = null)
        {
            _responder = responder ?? (_ => new HttpResponseMessage(HttpStatusCode.OK));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new CapturedRequest(request.Method, request.RequestUri?.ToString() ?? string.Empty, body));
            return _responder(request);
        }
    }

    private sealed record CapturedRequest(HttpMethod Method, string Url, string Body);
}
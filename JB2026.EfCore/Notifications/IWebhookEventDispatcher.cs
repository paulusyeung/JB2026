namespace JB2026.EfCore.Notifications;

/// <summary>
/// Dispatches a webhook event to active subscriptions matching the event type.
/// Implementations may queue the dispatch (e.g. Hangfire) or run it inline.
/// </summary>
public interface IWebhookEventDispatcher
{
    Task EnqueueEventAsync(string eventType, object payload, CancellationToken cancellationToken);
}
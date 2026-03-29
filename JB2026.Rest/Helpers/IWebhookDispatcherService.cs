namespace JB2026.Rest.Helpers;

public interface IWebhookDispatcherService
{
    Task EnqueueEventAsync(string eventType, object payload, CancellationToken cancellationToken);
}
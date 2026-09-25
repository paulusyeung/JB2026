using JB2026.EfCore.Notifications;

namespace JB2026.Rest.Helpers;

public sealed class FcmEventHelperService : IFcmEventHelperService
{
    private readonly JobLifecycleEventPublisher _publisher;

    public FcmEventHelperService(JobLifecycleEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task NotifyReadyPaperAsync(Guid orderId, CancellationToken cancellationToken)
        => _publisher.PublishOrderEventAsync(JobLifecycleEventType.ReadyPaper, orderId, cancellationToken);

    public Task NotifyReadyPlateAsync(Guid orderId, CancellationToken cancellationToken)
        => _publisher.PublishOrderEventAsync(JobLifecycleEventType.ReadyPlate, orderId, cancellationToken);
}
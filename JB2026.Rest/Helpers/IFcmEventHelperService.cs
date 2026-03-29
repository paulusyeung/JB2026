namespace JB2026.Rest.Helpers;

public interface IFcmEventHelperService
{
    Task NotifyReadyPaperAsync(Guid orderId, CancellationToken cancellationToken);

    Task NotifyReadyPlateAsync(Guid orderId, CancellationToken cancellationToken);
}
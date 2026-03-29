using JB2026.EfCore.Data;
using JB2026.EfCore.Models;

namespace JB2026.Rest.Helpers;

public sealed class FcmEventHelperService : IFcmEventHelperService
{
    private readonly JB5LegacyWriteContext _writeContext;
    private readonly IWebhookDispatcherService _webhookDispatcher;

    public FcmEventHelperService(JB5LegacyWriteContext writeContext, IWebhookDispatcherService webhookDispatcher)
    {
        _writeContext = writeContext;
        _webhookDispatcher = webhookDispatcher;
    }

    public Task NotifyReadyPaperAsync(Guid orderId, CancellationToken cancellationToken)
        => AddHistoryAndDispatchAsync("OnReadyPaper", orderId, cancellationToken);

    public Task NotifyReadyPlateAsync(Guid orderId, CancellationToken cancellationToken)
        => AddHistoryAndDispatchAsync("OnReadyPlate", orderId, cancellationToken);

    private async Task AddHistoryAndDispatchAsync(string topic, Guid orderId, CancellationToken cancellationToken)
    {
        var createdOn = DateTime.Now;

        _writeContext.FCMHistories.Add(new FCMHistory
        {
            FCMHistoryId = Guid.NewGuid(),
            MessageTitle = topic,
            MessageBody = orderId.ToString(),
            DeliveredOn = createdOn,
            Topic = topic,
            RecipientList = "staffonly",
            UserIdList = string.Empty
        });

        await _writeContext.SaveChangesAsync(cancellationToken);

        await _webhookDispatcher.EnqueueEventAsync(topic, new
        {
            Topic = topic,
            OrderId = orderId,
            CreatedOn = createdOn
        }, cancellationToken);
    }
}
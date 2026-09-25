using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;

namespace JB2026.EfCore.Notifications;

/// <summary>
/// Writes legacy-format FCMHistory rows (Topic="Device") and dispatches
/// webhook events for job lifecycle events. No real FCM/device delivery
/// is performed — history records and webhooks only.
/// </summary>
public sealed class JobLifecycleEventPublisher
{
    private const int FcmRegistrationAuthType = 3;
    private const string StaffOnlyRecipients = "staffonly";

    private readonly JB5LegacyWriteContext _writeContext;
    private readonly JB5LegacyReadContext _readContext;
    private readonly IWebhookEventDispatcher _webhookDispatcher;

    public JobLifecycleEventPublisher(
        JB5LegacyWriteContext writeContext,
        JB5LegacyReadContext readContext,
        IWebhookEventDispatcher webhookDispatcher)
    {
        _writeContext = writeContext;
        _readContext = readContext;
        _webhookDispatcher = webhookDispatcher;
    }

    private static readonly IReadOnlyDictionary<JobLifecycleEventType, string> LegacyTitles =
        new Dictionary<JobLifecycleEventType, string>
        {
            [JobLifecycleEventType.OrderCreated] = "JB5 新增訂單",
            [JobLifecycleEventType.Scheduled] = "JB5 已排單",
            [JobLifecycleEventType.ReadyPlate] = "JB5 有鋅",
            [JobLifecycleEventType.ReadyPaper] = "JB5 有紙",
            [JobLifecycleEventType.Completed] = "JB5 全單完成",
            [JobLifecycleEventType.Invoiced] = "JB5 已開發票",
            [JobLifecycleEventType.CogsFilled] = "JB5 已填成本",
        };

    private static readonly IReadOnlyDictionary<JobLifecycleEventType, string> WebhookEventTypes =
        new Dictionary<JobLifecycleEventType, string>
        {
            [JobLifecycleEventType.OrderCreated] = "OnJobCreated",
            [JobLifecycleEventType.Scheduled] = "OnJobScheduled",
            [JobLifecycleEventType.ReadyPlate] = "OnReadyPlate",
            [JobLifecycleEventType.ReadyPaper] = "OnReadyPaper",
            [JobLifecycleEventType.Completed] = "OnJobCompleted",
            [JobLifecycleEventType.Invoiced] = "OnJobInvoiced",
            [JobLifecycleEventType.CogsFilled] = "OnJobCogsFilled",
        };

    private static readonly IReadOnlyDictionary<JobLifecycleEventType, int> NotifyTypes =
        new Dictionary<JobLifecycleEventType, int>
        {
            [JobLifecycleEventType.OrderCreated] = 10,
            [JobLifecycleEventType.Scheduled] = 11,
            [JobLifecycleEventType.ReadyPaper] = 12,
            [JobLifecycleEventType.ReadyPlate] = 13,
            [JobLifecycleEventType.Completed] = 14,
            [JobLifecycleEventType.Invoiced] = 15,
            [JobLifecycleEventType.CogsFilled] = 16,
        };

    /// <summary>
    /// Publishes a job-scoped lifecycle event for a job order, resolving the
    /// target owner from the order and composing the legacy message body.
    /// </summary>
    public Task PublishOrderEventAsync(JobLifecycleEventType eventType, Guid orderId, CancellationToken cancellationToken = default)
    {
        return PublishAsync(async ct =>
        {
            var order = await _writeContext.JobOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.OrderId == orderId, ct);

            Guid? ownerId = null;
            string body;
            if (order is not null)
            {
                ownerId = await ResolveOwnerAsync(order, ct);
                body = ComposeOrderBody(order);
            }
            else
            {
                body = orderId.ToString();
            }

            return (eventType, ownerId, body, new
            {
                EventType = WebhookEventTypes[eventType],
                Topic = "Device",
                OrderId = orderId,
                CreatedOn = DateTime.Now,
            });
        }, cancellationToken);
    }

    private async Task PublishAsync(
        Func<CancellationToken, Task<(JobLifecycleEventType EventType, Guid? TargetUserId, string Body, object Payload)>> compose,
        CancellationToken cancellationToken)
    {
        var (eventType, targetUserId, body, payload) = await compose(cancellationToken);
        var createdOn = DateTime.Now;

        var recipients = await ResolveRecipientsAsync(eventType, targetUserId, cancellationToken);

        _writeContext.FCMHistories.Add(new FCMHistory
        {
            FCMHistoryId = Guid.NewGuid(),
            MessageTitle = LegacyTitles[eventType],
            MessageBody = body,
            DeliveredOn = createdOn,
            Topic = "Device",
            RecipientList = recipients.RecipientList,
            UserIdList = recipients.UserIdList,
        });

        await _writeContext.SaveChangesAsync(cancellationToken);
        await _webhookDispatcher.EnqueueEventAsync(WebhookEventTypes[eventType], payload, cancellationToken);
    }

    private async Task<Guid?> ResolveOwnerAsync(JobOrder order, CancellationToken cancellationToken)
    {
        if (order.CreatedBy != Guid.Empty)
        {
            return order.CreatedBy;
        }

        if (!string.IsNullOrWhiteSpace(order.OrderedBy))
        {
            return await _readContext.vwUserList_Actives
                .AsNoTracking()
                .Where(u => u.UserName == order.OrderedBy || u.UserAlias == order.OrderedBy)
                .Select(u => (Guid?)u.UserId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    private async Task<(string RecipientList, string UserIdList)> ResolveRecipientsAsync(
        JobLifecycleEventType eventType,
        Guid? targetUserId,
        CancellationToken cancellationToken)
    {
        if (targetUserId.HasValue)
        {
            var userId = targetUserId.Value;
            var devices = await _writeContext.UserAuths
                .AsNoTracking()
                .Where(auth => auth.UserId == userId && auth.AuthType == FcmRegistrationAuthType)
                .Select(auth => auth.DeviceId)
                .ToListAsync(cancellationToken);

            var optedIn = await _writeContext.UserNotifications
                .AsNoTracking()
                .Where(notification => notification.UserId == userId && notification.NotifyType == NotifyTypes[eventType])
                .Select(notification => notification.DeviceId)
                .ToListAsync(cancellationToken);

            var targeted = optedIn.Count > 0 ? optedIn : devices;
            if (targeted.Count > 0)
            {
                return (
                    string.Join(',', targeted),
                    string.Join(',', Enumerable.Repeat(userId.ToString(), targeted.Count)));
            }
        }

        return (StaffOnlyRecipients, string.Empty);
    }

    private static string ComposeOrderBody(JobOrder order)
    {
        var composite = order.JobNumber.HasValue
            ? $"{order.OrderNumber}-{order.JobNumber.Value}"
            : order.OrderNumber;

        return $"{composite}: {order.CustomerName}".TrimEnd();
    }
}
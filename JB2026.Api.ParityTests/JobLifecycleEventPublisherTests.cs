using System.Text.Json;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using JB2026.EfCore.Notifications;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

/// <summary>
/// Unit tests for <see cref="JobLifecycleEventPublisher"/>: legacy FCMHistory
/// row composition, recipient targeting/fallback, title mapping, and webhook
/// dispatch for all job lifecycle events.
/// </summary>
public sealed class JobLifecycleEventPublisherTests
{
    private static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static JB5LegacyWriteContext CreateWriteContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<JB5LegacyWriteContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new JB5LegacyWriteContext(options);
    }

    private static JB5LegacyReadContext CreateReadContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<JB5LegacyReadContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new JB5LegacyReadContext(options);
    }

    private static JobLifecycleEventPublisher CreatePublisher(
        string dbName,
        RecordingWebhookDispatcher dispatcher)
    {
        return new JobLifecycleEventPublisher(
            CreateWriteContext(dbName),
            CreateReadContext(dbName),
            dispatcher);
    }

    private static JobOrder CreateOrder(
        string orderNumber = "ORD",
        int? jobNumber = 5,
        string customerName = "Acme",
        Guid? createdBy = null)
    {
        return new JobOrder
        {
            OrderId = Guid.NewGuid(),
            OrderType = 0,
            OrderNumber = orderNumber,
            JobNumber = jobNumber,
            CustomerName = customerName,
            CreatedBy = createdBy ?? OwnerId,
        };
    }

    // -----------------------------------------------------------------------
    // Order events — recipient targeting and body composition
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PublishOrderEventAsync_OptedInDevice_TargetsOwnerDevices()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var dispatcher = new RecordingWebhookDispatcher();
        var order = CreateOrder();
        var otherDevice = new UserAuth { AuthId = Guid.NewGuid(), UserId = OwnerId, AuthType = 3, DeviceId = "device-registered" };
        var optedDevice = new UserNotification { NotifyId = Guid.NewGuid(), UserId = OwnerId, NotifyType = 10, DeviceId = "device-opted" };

        using (var context = CreateWriteContext(dbName))
        {
            context.JobOrders.Add(order);
            context.UserAuths.Add(otherDevice);
            context.UserNotifications.Add(optedDevice);
            await context.SaveChangesAsync();
        }

        var publisher = CreatePublisher(dbName, dispatcher);
        await publisher.PublishOrderEventAsync(JobLifecycleEventType.OrderCreated, order.OrderId, CancellationToken.None);

        using var verify = CreateWriteContext(dbName);
        var row = await verify.FCMHistories.SingleAsync();

        Assert.Equal("Device", row.Topic);
        Assert.Equal("JB5 新增訂單", row.MessageTitle);
        Assert.Equal("ORD-5: Acme", row.MessageBody);
        Assert.Equal("device-opted", row.RecipientList);
        Assert.Equal(OwnerId.ToString(), row.UserIdList);
    }

    [Fact]
    public async Task PublishOrderEventAsync_NoOptIn_FallsBackToRegisteredDevices()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var dispatcher = new RecordingWebhookDispatcher();
        var order = CreateOrder();

        using (var context = CreateWriteContext(dbName))
        {
            context.JobOrders.Add(order);
            context.UserAuths.Add(new UserAuth { AuthId = Guid.NewGuid(), UserId = OwnerId, AuthType = 3, DeviceId = "device-registered" });
            context.UserAuths.Add(new UserAuth { AuthId = Guid.NewGuid(), UserId = OwnerId, AuthType = 3, DeviceId = "device-registered-2" });
            await context.SaveChangesAsync();
        }

        var publisher = CreatePublisher(dbName, dispatcher);
        await publisher.PublishOrderEventAsync(JobLifecycleEventType.Scheduled, order.OrderId, CancellationToken.None);

        using var verify = CreateWriteContext(dbName);
        var row = await verify.FCMHistories.SingleAsync();

        Assert.Equal("JB5 已排單", row.MessageTitle);
        Assert.Equal("device-registered,device-registered-2", row.RecipientList);
        Assert.Equal($"{OwnerId},{OwnerId}", row.UserIdList);
    }

    [Fact]
    public async Task PublishOrderEventAsync_NoOwnerDevices_FallsBackToStaffOnly()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var dispatcher = new RecordingWebhookDispatcher();
        var order = CreateOrder(createdBy: Guid.Empty);

        using (var context = CreateWriteContext(dbName))
        {
            context.JobOrders.Add(order);
            await context.SaveChangesAsync();
        }

        var publisher = CreatePublisher(dbName, dispatcher);
        await publisher.PublishOrderEventAsync(JobLifecycleEventType.ReadyPlate, order.OrderId, CancellationToken.None);

        using var verify = CreateWriteContext(dbName);
        var row = await verify.FCMHistories.SingleAsync();

        Assert.Equal("Device", row.Topic);
        Assert.Equal("JB5 有鋅", row.MessageTitle);
        Assert.Equal("staffonly", row.RecipientList);
        Assert.Equal(string.Empty, row.UserIdList);
    }

    [Fact]
    public async Task PublishOrderEventAsync_MissingOrder_UsesOrderIdAsBody()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var dispatcher = new RecordingWebhookDispatcher();
        var publisher = CreatePublisher(dbName, dispatcher);
        var missingOrderId = Guid.NewGuid();

        await publisher.PublishOrderEventAsync(JobLifecycleEventType.ReadyPaper, missingOrderId, CancellationToken.None);

        using var verify = CreateWriteContext(dbName);
        var row = await verify.FCMHistories.SingleAsync();
        Assert.Equal("JB5 有紙", row.MessageTitle);
        Assert.Equal(missingOrderId.ToString(), row.MessageBody);
        Assert.Equal("staffonly", row.RecipientList);
    }

    // -----------------------------------------------------------------------
    // COGS-filled event (job-order COGS / OriginalSONumber)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PublishOrderEventAsync_CogsFilled_TargetsOwnerAndComposesOrderBody()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var dispatcher = new RecordingWebhookDispatcher();
        var order = CreateOrder(createdBy: OwnerId);

        using (var context = CreateWriteContext(dbName))
        {
            context.JobOrders.Add(order);
            context.UserAuths.Add(new UserAuth { AuthId = Guid.NewGuid(), UserId = OwnerId, AuthType = 3, DeviceId = "device-registered" });
            context.UserNotifications.Add(new UserNotification { NotifyId = Guid.NewGuid(), UserId = OwnerId, NotifyType = 16, DeviceId = "device-cogs-opted" });
            await context.SaveChangesAsync();
        }

        var publisher = CreatePublisher(dbName, dispatcher);
        await publisher.PublishOrderEventAsync(JobLifecycleEventType.CogsFilled, order.OrderId, CancellationToken.None);

        using var verify = CreateWriteContext(dbName);
        var row = await verify.FCMHistories.SingleAsync();

        Assert.Equal("Device", row.Topic);
        Assert.Equal("JB5 已填成本", row.MessageTitle);
        Assert.Equal("ORD-5: Acme", row.MessageBody);
        Assert.Equal("device-cogs-opted", row.RecipientList);
        Assert.Equal(OwnerId.ToString(), row.UserIdList);
    }

    [Fact]
    public async Task PublishOrderEventAsync_CogsFilled_NoOwnerDevices_FallsBackToStaffOnlyAndDispatchesWebhook()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var dispatcher = new RecordingWebhookDispatcher();
        var order = CreateOrder(createdBy: Guid.Empty);

        using (var context = CreateWriteContext(dbName))
        {
            context.JobOrders.Add(order);
            await context.SaveChangesAsync();
        }

        var publisher = CreatePublisher(dbName, dispatcher);
        await publisher.PublishOrderEventAsync(JobLifecycleEventType.CogsFilled, order.OrderId, CancellationToken.None);

        using var verify = CreateWriteContext(dbName);
        var row = await verify.FCMHistories.SingleAsync();
        Assert.Equal("staffonly", row.RecipientList);

        var dispatched = dispatcher.Events.Single();
        Assert.Equal("OnJobCogsFilled", dispatched.EventType);
        var payload = JsonSerializer.Deserialize<Dictionary<string, object?>>(dispatched.PayloadJson);
        Assert.NotNull(payload);
        Assert.Equal("OnJobCogsFilled", payload["EventType"]?.ToString());
        Assert.Equal("Device", payload["Topic"]?.ToString());
        Assert.Equal(order.OrderId.ToString(), payload["OrderId"]?.ToString());
    }

    // -----------------------------------------------------------------------
    // Title/webhook mapping across all events
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PublishAsync_AllEventTypes_MapsLegacyTitlesAndWebhookNames()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var dispatcher = new RecordingWebhookDispatcher();
        var order = CreateOrder(createdBy: Guid.Empty);

        using (var context = CreateWriteContext(dbName))
        {
            context.JobOrders.Add(order);
            await context.SaveChangesAsync();
        }

        var orderEvents = new (JobLifecycleEventType EventType, string Title, string Webhook)[]
        {
            (JobLifecycleEventType.OrderCreated, "JB5 新增訂單", "OnJobCreated"),
            (JobLifecycleEventType.Scheduled, "JB5 已排單", "OnJobScheduled"),
            (JobLifecycleEventType.ReadyPlate, "JB5 有鋅", "OnReadyPlate"),
            (JobLifecycleEventType.ReadyPaper, "JB5 有紙", "OnReadyPaper"),
            (JobLifecycleEventType.Completed, "JB5 全單完成", "OnJobCompleted"),
            (JobLifecycleEventType.Invoiced, "JB5 已開發票", "OnJobInvoiced"),
            (JobLifecycleEventType.CogsFilled, "JB5 已填成本", "OnJobCogsFilled"),
        };

        var publisher = CreatePublisher(dbName, dispatcher);
        foreach (var (eventType, _, _) in orderEvents)
        {
            await publisher.PublishOrderEventAsync(eventType, order.OrderId, CancellationToken.None);
        }

        using var verify = CreateWriteContext(dbName);
        var rows = await verify.FCMHistories.ToListAsync();

        Assert.Equal(orderEvents.Length, rows.Count);
        foreach (var (_, title, webhook) in orderEvents)
        {
            Assert.Contains(rows, row => row.MessageTitle == title && row.Topic == "Device");
            Assert.Contains(dispatcher.Events, dispatched => dispatched.EventType == webhook);
        }
    }

    private sealed class RecordingWebhookDispatcher : IWebhookEventDispatcher
    {
        public List<(string EventType, string PayloadJson)> Events { get; } = [];

        public Task EnqueueEventAsync(string eventType, object payload, CancellationToken cancellationToken)
        {
            Events.Add((eventType, JsonSerializer.Serialize(payload)));
            return Task.CompletedTask;
        }
    }
}
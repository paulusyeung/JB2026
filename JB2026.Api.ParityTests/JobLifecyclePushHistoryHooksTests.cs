using System.Text.Json;
using JB2026.Api.Controllers;
using JB2026.Api.Models;
using JB2026.Api.Models.Billing;
using JB2026.Api.Options;
using JB2026.Api.Services;
using JB2026.Api.Services.Billing;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using JB2026.EfCore.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JB2026.Api.ParityTests;

/// <summary>
/// Integration-style tests covering the lifecycle push hooks: FCMHistory
/// records and webhook dispatches emitted by the production (JB2026.Api)
/// hot paths — job creation, scheduling, completion, invoicing, and job
/// COGS (OriginalSONumber) filling.
/// </summary>
public sealed class JobLifecyclePushHistoryHooksTests
{
    private static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string OwnerIdText = "11111111-1111-1111-1111-111111111111";

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

    private static string NewDbName() => Guid.NewGuid().ToString("N");

    private static JobLifecycleEventPublisher CreatePublisher(string dbName, IWebhookEventDispatcher dispatcher)
        => new(CreateWriteContext(dbName), CreateReadContext(dbName), dispatcher, NullLogger<JobLifecycleEventPublisher>.Instance);

    private static EfJobManagementRepository CreateRepository(
        string dbName,
        RecordingWebhookDispatcher dispatcher)
    {
        return new EfJobManagementRepository(
            CreateReadContext(dbName),
            CreateWriteContext(dbName),
            NullLogger<EfJobManagementRepository>.Instance,
            CreatePublisher(dbName, dispatcher));
    }

    private static JobSchedulesController CreateSchedulesController(
        string dbName,
        IWebhookEventDispatcher dispatcher)
        => CreateSchedulesController(dbName, dispatcher, new NoOpScheduleGateway());

    private static JobSchedulesController CreateSchedulesController(
        string dbName,
        IWebhookEventDispatcher dispatcher,
        IJobScheduleStoredProcedureGateway scheduleGateway)
    {
        var controller = new JobSchedulesController(
            CreateReadContext(dbName),
            CreateWriteContext(dbName),
            scheduleGateway,
            new NoOpPackingGateway(),
            CreatePublisher(dbName, dispatcher));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static JobOrder CreateOrder(string orderNumber = "O-100", int? jobNumber = 3, string customerName = "Acme Corp")
    {
        return new JobOrder
        {
            OrderId = Guid.NewGuid(),
            OrderType = 0,
            OrderNumber = orderNumber,
            JobNumber = jobNumber,
            CustomerName = customerName,
            Status = 1,
            CreatedBy = OwnerId,
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = OwnerId,
            ModifiedOn = DateTime.UtcNow,
            Retired = false,
            RetiredOn = new DateTime(1900, 1, 1),
            RetiredBy = Guid.Empty,
        };
    }

    private static async Task<Guid> SeedOrderAsync(string dbName, JobOrder order)
    {
        using var context = CreateWriteContext(dbName);
        context.JobOrders.Add(order);
        await context.SaveChangesAsync();
        return order.OrderId;
    }

    private static async Task SeedOwnerDevicesAsync(string dbName)
    {
        using var context = CreateWriteContext(dbName);
        context.UserAuths.Add(new UserAuth { UserId = OwnerId, AuthType = 3, DeviceId = "device-reg" });
        context.UserNotifications.Add(new UserNotification { UserId = OwnerId, NotifyType = 10, DeviceId = "device-opt" });
        await context.SaveChangesAsync();
    }

    // -----------------------------------------------------------------------
    // Job created
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateJobOrder_PublishesCreatedHistoryAndWebhook()
    {
        var dbName = NewDbName();
        await SeedOwnerDevicesAsync(dbName);
        var dispatcher = new RecordingWebhookDispatcher();
        var repository = CreateRepository(dbName, dispatcher);

        var response = await repository.CreateJobOrder(new CreateJobOrderRequest
        {
            OrderNumber = "O-100",
            JobNumber = "3",
            CustomerName = "Acme Corp",
            OrderTitle = "Gift box",
            OrderedBy = "admin",
            OrderedOn = new DateTime(2026, 1, 5),
            RequiredOn = new DateTime(2026, 1, 10),
            Qty = 5000,
            Status = 1,
            OrderType = 0,
        }, OwnerIdText);

        Assert.NotNull(response);
        Assert.Equal("OnJobCreated", dispatcher.Events.Single().EventType);

        using var verify = CreateWriteContext(dbName);
        var row = await verify.FCMHistories.SingleAsync();
        Assert.Equal("Device", row.Topic);
        Assert.Equal("JB5 新增訂單", row.MessageTitle);
        Assert.Equal("O-100-3: Acme Corp", row.MessageBody);
        Assert.Equal("device-opt", row.RecipientList);
        Assert.Equal(OwnerIdText, row.UserIdList);
    }

    // -----------------------------------------------------------------------
    // Scheduled
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SaveBatch_ScheduledItem_PublishesScheduled()
    {
        var dbName = NewDbName();
        var orderId = await SeedOrderAsync(dbName, CreateOrder());
        var dispatcher = new RecordingWebhookDispatcher();
        var controller = CreateSchedulesController(dbName, dispatcher);

        var result = await controller.SaveBatch(new SaveScheduleBatchRequest
        {
            OrderType = 0,
            ScheduledItems =
            [
                new SaveScheduleBatchItem { OrderId = orderId, MachineNumber = "M-1", Step1Status = 0, UrgencyLevel = 0 },
            ],
        }, CancellationToken.None);

        Assert.IsAssignableFrom<OkObjectResult>(result);
        Assert.Equal("OnJobScheduled", dispatcher.Events.Single().EventType);

        using var verify = CreateWriteContext(dbName);
        var row = Assert.Single(await verify.FCMHistories.ToListAsync());
        Assert.Equal("JB5 已排單", row.MessageTitle);
        Assert.Equal("O-100-3: Acme Corp", row.MessageBody);
    }

    // -----------------------------------------------------------------------
    // Scheduled — webhook dispatch failure must not fail the save
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SaveBatch_Reorder_SucceedsAndPersistsPriority_WhenWebhookDispatchThrows()
    {
        // Regression: dbo.WebhookSubscriptions did not exist, so the dispatcher's
        // subscription read threw AFTER spJobSchedule_UpdRec had already run.
        // SaveBatch returned 500 and ScheduleView reported "Unable to save
        // schedule" even though the reorder had been committed.
        var dbName = NewDbName();
        var first = CreateOrder("170287", 1);
        var moved = CreateOrder("170288", 1);
        var last = CreateOrder("170289", 1);
        await SeedOrderAsync(dbName, first);
        await SeedOrderAsync(dbName, moved);
        await SeedOrderAsync(dbName, last);
        await SeedSchedulesAsync(dbName, first.OrderId, moved.OrderId, last.OrderId);

        var dispatcher = new ThrowingWebhookDispatcher();
        var gateway = new RecordingScheduleGateway();
        var controller = CreateSchedulesController(dbName, dispatcher, gateway);

        // Row 2 (moved) is dragged down to row 3.
        var result = await controller.SaveBatch(new SaveScheduleBatchRequest
        {
            OrderType = 0,
            ScheduledItems =
            [
                new SaveScheduleBatchItem { OrderId = first.OrderId, MachineNumber = "1", UrgencyLevel = 0 },
                new SaveScheduleBatchItem { OrderId = last.OrderId, MachineNumber = "1", UrgencyLevel = 0 },
                new SaveScheduleBatchItem { OrderId = moved.OrderId, MachineNumber = "1", UrgencyLevel = 0 },
            ],
        }, CancellationToken.None);

        Assert.IsAssignableFrom<OkObjectResult>(result);

        // The reordered sequence reached the stored procedure as 0-based priority.
        Assert.Collection(
            gateway.Updates,
            u => AssertUpdate(u, first.OrderId, expectedPriority: 0),
            u => AssertUpdate(u, last.OrderId, expectedPriority: 1),
            u => AssertUpdate(u, moved.OrderId, expectedPriority: 2));

        // Push history is still recorded even though dispatch blew up — one row
        // per scheduled item, in the reordered sequence.
        using var verify = CreateWriteContext(dbName);
        var history = await verify.FCMHistories.OrderBy(h => h.DeliveredOn).ToListAsync();
        Assert.Equal(3, history.Count);
        Assert.All(history, h => Assert.Equal("JB5 已排單", h.MessageTitle));
        Assert.Equal(
            ["170287-1: Acme Corp", "170289-1: Acme Corp", "170288-1: Acme Corp"],
            history.Select(h => h.MessageBody));
    }

    private static void AssertUpdate(
        UpdateJobScheduleStoredProcedureRequest update,
        Guid expectedOrderId,
        int expectedPriority)
    {
        Assert.Equal(expectedOrderId, update.OrderId);
        Assert.Equal(expectedPriority, update.Priority);
    }

    private static async Task SeedSchedulesAsync(string dbName, params Guid[] orderIds)
    {
        using var context = CreateWriteContext(dbName);
        for (var i = 0; i < orderIds.Length; i++)
        {
            context.JobSchedules.Add(new JobSchedule
            {
                ScheduleId = Guid.NewGuid(),
                OrderId = orderIds[i],
                ScheduledOn = new DateTime(2026, 9, 26),
                Status = 0,
                Priority = i,
                MachineNumber = "1",
                Cancelled = false,
                UrgencyLevel = 0,
            });
        }

        await context.SaveChangesAsync();
    }

    // -----------------------------------------------------------------------
    // Completed
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SaveBatch_CompletedTransition_PublishesExactlyOnce()
    {
        var dbName = NewDbName();
        var orderId = await SeedOrderAsync(dbName, CreateOrder());
        var dispatcher = new RecordingWebhookDispatcher();
        var controller = CreateSchedulesController(dbName, dispatcher);
        var request = new SaveScheduleBatchRequest { CompletedOrderIds = [orderId] };

        await controller.SaveBatch(request, CancellationToken.None);
        await controller.SaveBatch(request, CancellationToken.None);

        Assert.Single(dispatcher.Events.Where(e => e.EventType == "OnJobCompleted"));

        using var verify = CreateWriteContext(dbName);
        Assert.Single(await verify.FCMHistories.Where(r => r.MessageTitle == "JB5 全單完成").ToListAsync());
    }

    [Fact]
    public async Task UpdateJobOrder_CompletedTransition_PublishesExactlyOnce()
    {
        var dbName = NewDbName();
        var orderId = await SeedOrderAsync(dbName, CreateOrder());
        var dispatcher = new RecordingWebhookDispatcher();
        var repository = CreateRepository(dbName, dispatcher);
        var request = CreateUpdateRequest(completedOn: new DateTime(2026, 1, 12));

        var first = await repository.UpdateJobOrder(orderId, request, OwnerIdText);
        Assert.NotNull(first);
        Assert.Single(dispatcher.Events.Where(e => e.EventType == "OnJobCompleted"));

        await repository.UpdateJobOrder(orderId, request, OwnerIdText);
        Assert.Single(dispatcher.Events.Where(e => e.EventType == "OnJobCompleted"));

        using var verify = CreateWriteContext(dbName);
        Assert.Single(await verify.FCMHistories.Where(r => r.MessageTitle == "JB5 全單完成").ToListAsync());
    }

    // -----------------------------------------------------------------------
    // Invoiced — form save path (InvoiceRef empty→set)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateJobOrder_InvoicedTransition_PublishesExactlyOnce()
    {
        var dbName = NewDbName();
        var orderId = await SeedOrderAsync(dbName, CreateOrder());
        var dispatcher = new RecordingWebhookDispatcher();
        var repository = CreateRepository(dbName, dispatcher);
        var request = CreateUpdateRequest(invoiceRef: "INV-001", invoiceAmount: 1200m);

        var first = await repository.UpdateJobOrder(orderId, request, OwnerIdText);
        Assert.NotNull(first);
        Assert.Single(dispatcher.Events.Where(e => e.EventType == "OnJobInvoiced"));

        await repository.UpdateJobOrder(orderId, request, OwnerIdText);
        Assert.Single(dispatcher.Events.Where(e => e.EventType == "OnJobInvoiced"));

        using var verify = CreateWriteContext(dbName);
        Assert.Single(await verify.FCMHistories.Where(r => r.MessageTitle == "JB5 已開發票").ToListAsync());
    }

    // -----------------------------------------------------------------------
    // Invoiced — mark-sent path (invoice data persisted to the job changes)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SendInvoiceAsync_PublishesInvoicedWhenInvoiceDataChanges()
    {
        var dbName = NewDbName();
        var order = CreateOrder();
        order.InvoiceRef = "ext-001";
        await SeedOrderAsync(dbName, order);

        var dispatcher = new RecordingWebhookDispatcher();
        var publisher = CreatePublisher(dbName, dispatcher);
        var service = CreateBillingService(dbName, new MarkSentInvoiceNinjaHttpClient(), publisher);

        await service.SendInvoiceAsync("ext-001", OwnerId);
        await service.SendInvoiceAsync("ext-001", OwnerId);

        Assert.Single(dispatcher.Events.Where(e => e.EventType == "OnJobInvoiced"));

        using var verify = CreateWriteContext(dbName);
        Assert.Single(await verify.FCMHistories.Where(r => r.MessageTitle == "JB5 已開發票").ToListAsync());
        var updated = await verify.JobOrders.SingleAsync();
        Assert.Equal("INV-001", updated.InvoiceRef);
        Assert.Equal(100m, updated.InvoiceAmount);
    }

    // -----------------------------------------------------------------------
    // COGS filled — job-order OriginalSONumber (the JobOrderForm COGS field)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateJobOrder_WithOriginalSO_PublishesCogsHistory()
    {
        var dbName = NewDbName();
        await SeedOwnerDevicesAsync(dbName);
        using (var optIn = CreateWriteContext(dbName))
        {
            optIn.UserNotifications.Add(new UserNotification { NotifyId = Guid.NewGuid(), UserId = OwnerId, NotifyType = 16, DeviceId = "device-cogs-opt" });
            await optIn.SaveChangesAsync();
        }
        var dispatcher = new RecordingWebhookDispatcher();
        var repository = CreateRepository(dbName, dispatcher);

        var response = await repository.CreateJobOrder(new CreateJobOrderRequest
        {
            OrderNumber = "O-100",
            JobNumber = "3",
            CustomerName = "Acme Corp",
            OrderTitle = "Gift box",
            OrderedBy = "admin",
            OrderedOn = new DateTime(2026, 1, 5),
            RequiredOn = new DateTime(2026, 1, 10),
            Qty = 5000,
            Status = 1,
            OrderType = 0,
            OriginalSONumber = "820.50",
        }, OwnerIdText);

        Assert.NotNull(response);
        Assert.Equal("OnJobCreated", dispatcher.Events.First().EventType);
        Assert.Equal("OnJobCogsFilled", dispatcher.Events.Last().EventType);

        using var verify = CreateWriteContext(dbName);
        var rows = await verify.FCMHistories.ToListAsync();
        var cogs = Assert.Single(rows.Where(r => r.MessageTitle == "JB5 已填成本"));
        Assert.Equal("O-100-3: Acme Corp", cogs.MessageBody);
        Assert.Equal("device-cogs-opt", cogs.RecipientList);
        Assert.Equal(OwnerIdText, cogs.UserIdList);
    }

    [Fact]
    public async Task CreateJobOrder_WithoutOriginalSO_DoesNotPublishCogs()
    {
        var dbName = NewDbName();
        var dispatcher = new RecordingWebhookDispatcher();
        var repository = CreateRepository(dbName, dispatcher);

        var response = await repository.CreateJobOrder(new CreateJobOrderRequest
        {
            OrderNumber = "O-100",
            JobNumber = "3",
            CustomerName = "Acme Corp",
            OrderTitle = "Gift box",
            OrderedBy = "admin",
            OrderedOn = new DateTime(2026, 1, 5),
            RequiredOn = new DateTime(2026, 1, 10),
            Qty = 5000,
            Status = 1,
            OrderType = 0,
        }, OwnerIdText);

        Assert.NotNull(response);
        Assert.Single(dispatcher.Events);
        Assert.Equal("OnJobCreated", dispatcher.Events.Single().EventType);

        using var verify = CreateWriteContext(dbName);
        Assert.Single(await verify.FCMHistories.ToListAsync());
    }

    [Fact]
    public async Task UpdateJobOrder_OriginalSoChange_PublishesCogsPerChange()
    {
        var dbName = NewDbName();
        var orderId = await SeedOrderAsync(dbName, CreateOrder());
        var dispatcher = new RecordingWebhookDispatcher();
        var repository = CreateRepository(dbName, dispatcher);
        var request = CreateUpdateRequest(originalSONumber: "820.50");

        var first = await repository.UpdateJobOrder(orderId, request, OwnerIdText);
        Assert.NotNull(first);
        Assert.Single(dispatcher.Events.Where(e => e.EventType == "OnJobCogsFilled"));

        await repository.UpdateJobOrder(orderId, request, OwnerIdText);
        Assert.Single(dispatcher.Events.Where(e => e.EventType == "OnJobCogsFilled"));

        using var verify = CreateWriteContext(dbName);
        Assert.Single(await verify.FCMHistories.Where(r => r.MessageTitle == "JB5 已填成本").ToListAsync());

        await repository.UpdateJobOrder(orderId, CreateUpdateRequest(originalSONumber: "999.00"), OwnerIdText);
        Assert.Equal(2, dispatcher.Events.Count(e => e.EventType == "OnJobCogsFilled"));
        Assert.Equal(2, await verify.FCMHistories.CountAsync(r => r.MessageTitle == "JB5 已填成本"));
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static UpdateJobOrderRequest CreateUpdateRequest(
        DateTime? completedOn = null,
        string? invoiceRef = null,
        decimal? invoiceAmount = null,
        string? originalSONumber = null)
    {
        return new UpdateJobOrderRequest
        {
            OrderNumber = "O-100",
            CustomerName = "Acme Corp",
            OrderTitle = "Gift box",
            RequiredOn = new DateTime(2026, 1, 10),
            Qty = 5000,
            Remarks = string.Empty,
            Status = 1,
            OrderType = 0,
            CompletedOn = completedOn,
            InvoiceRef = invoiceRef,
            InvoiceAmount = invoiceAmount,
            OriginalSONumber = originalSONumber,
        };
    }

    private static BillingService CreateBillingService(
        string dbName,
        IInvoiceNinjaHttpClient invoiceNinjaHttpClient,
        JobLifecycleEventPublisher publisher)
    {
        var services = new ServiceCollection();
        services.AddSingleton(CreateReadContext(dbName));
        services.AddSingleton(CreateWriteContext(dbName));
        var provider = services.BuildServiceProvider();

        return new BillingService(
            invoiceNinjaHttpClient,
            Microsoft.Extensions.Options.Options.Create(new BillingOptions()),
            provider,
            NullLogger<BillingService>.Instance,
            publisher);
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

    private sealed class MarkSentInvoiceNinjaHttpClient : IInvoiceNinjaHttpClient
    {
        private readonly InvoiceNinjaInvoiceResponse _draft = new()
        {
            Id = "ext-001",
            Number = "INV-001",
            Amount = 100m,
            StatusId = "1",
        };

        public Task<T?> GetAsync<T>(string endpoint) where T : class
        {
            if (typeof(T) == typeof(InvoiceNinjaInvoiceResponse) && endpoint.StartsWith("/invoices/", StringComparison.Ordinal))
            {
                return Task.FromResult(_draft as T);
            }

            return Task.FromResult<T?>(null);
        }

        public Task<T> PostAsync<T>(string endpoint, object body) where T : class
            => Task.FromResult((T)(object)new { ok = true, data = Array.Empty<object>() });

        public Task<T> PutAsync<T>(string endpoint, object body) where T : class
            => throw new NotSupportedException();

        public Task<InvoiceNinjaBinaryResponse> PostStreamAsync(string endpoint, object body)
            => throw new NotSupportedException();

        public Task<bool> IsConnectedAsync() => Task.FromResult(true);

        public (bool isValid, string errorMessage) ValidateConfiguration() => (true, string.Empty);

        public Task<byte[]?> GetStreamAsync(string endpoint) => Task.FromResult<byte[]?>(null);
    }

    private sealed class ThrowingWebhookDispatcher : IWebhookEventDispatcher
    {
        /// <summary>
        /// Mirrors the production failure: the subscription read blew up because
        /// dbo.WebhookSubscriptions was missing.
        /// </summary>
        public Task EnqueueEventAsync(string eventType, object payload, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Invalid object name 'dbo.WebhookSubscriptions'.");
    }

    private sealed class RecordingScheduleGateway : IJobScheduleStoredProcedureGateway
    {
        public List<UpdateJobScheduleStoredProcedureRequest> Updates { get; } = [];

        public Task<JobScheduleStoredProcedureRecord?> SelectAsync(Guid scheduleId, CancellationToken cancellationToken = default)
            => Task.FromResult<JobScheduleStoredProcedureRecord?>(null);

        public Task<Guid> InsertAsync(CreateJobScheduleStoredProcedureRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(Guid.Empty);

        public Task<bool> UpdateAsync(UpdateJobScheduleStoredProcedureRequest request, CancellationToken cancellationToken = default)
        {
            Updates.Add(request);
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(Guid scheduleId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class NoOpScheduleGateway : IJobScheduleStoredProcedureGateway
    {
        public Task<JobScheduleStoredProcedureRecord?> SelectAsync(Guid scheduleId, CancellationToken cancellationToken = default)
            => Task.FromResult<JobScheduleStoredProcedureRecord?>(null);

        public Task<Guid> InsertAsync(CreateJobScheduleStoredProcedureRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(Guid.Empty);

        public Task<bool> UpdateAsync(UpdateJobScheduleStoredProcedureRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<bool> DeleteAsync(Guid scheduleId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class NoOpPackingGateway : IJobPackingOnAirStoredProcedureGateway
    {
        public Task<JobPackingOnAirStoredProcedureRecord?> SelectAsync(Guid onAirId, CancellationToken cancellationToken = default)
            => Task.FromResult<JobPackingOnAirStoredProcedureRecord?>(null);

        public Task<Guid> InsertAsync(CreateJobPackingOnAirStoredProcedureRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(Guid.Empty);

        public Task<bool> UpdateAsync(UpdateJobPackingOnAirStoredProcedureRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<bool> DeleteAsync(Guid onAirId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}
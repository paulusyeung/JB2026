using JB2026.Api.Controllers;
using JB2026.Api.Models;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

/// <summary>
/// Unit tests for <see cref="JobSchedulesController"/> using an in-memory EF Core store.
/// These tests exercise controller validation logic and response shape without a real database.
/// </summary>
public sealed class JobSchedulesControllerTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static JB5LegacyReadContext CreateReadContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<JB5LegacyReadContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new JB5LegacyReadContext(options);
    }

    private static JB5LegacyWriteContext CreateWriteContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<JB5LegacyWriteContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new JB5LegacyWriteContext(options);
    }

    private static JB5LegacyReadContext CreateContext(string dbName) => CreateReadContext(dbName);

    private static JobSchedulesController CreateController(
        JB5LegacyReadContext context,
        IJobScheduleStoredProcedureGateway gateway)
    {
        var writeOptions = new DbContextOptionsBuilder<JB5LegacyWriteContext>()
            .UseInMemoryDatabase(context.Database.GetDbConnection().Database)
            .Options;
        var writeContext = new JB5LegacyWriteContext(writeOptions);
        var controller = new JobSchedulesController(context, writeContext, gateway, new NoOpPackingGateway());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    private static JobSchedulesController CreateControllerWithWrite(
        JB5LegacyReadContext readContext,
        JB5LegacyWriteContext writeContext,
        IJobScheduleStoredProcedureGateway? gateway = null)
    {
        var controller = new JobSchedulesController(readContext, writeContext, gateway ?? new NoOpGateway(), new NoOpPackingGateway());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    // -----------------------------------------------------------------------
    // GET /range — validation
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(32)]
    [InlineData(100)]
    public async Task GetRange_InvalidDays_ReturnsBadRequest(int days)
    {
        using var context = CreateContext(nameof(GetRange_InvalidDays_ReturnsBadRequest) + days);
        var controller = CreateController(context, new NoOpGateway());

        var result = await controller.GetRange(DateOnly.FromDateTime(DateTime.Today), days, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(14)]
    [InlineData(31)]
    public async Task GetRange_ValidDaysNoData_ReturnsOkEmptyList(int days)
    {
        using var context = CreateContext(nameof(GetRange_ValidDaysNoData_ReturnsOkEmptyList) + days);
        var controller = CreateController(context, new NoOpGateway());

        var result = await controller.GetRange(DateOnly.FromDateTime(DateTime.Today), days, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<JobScheduleCalendarItemResponse>>(ok.Value);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetRange_ValidRange_ReturnsMappedItem()
    {
        using var context = CreateContext(nameof(GetRange_ValidRange_ReturnsMappedItem));

        var orderId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var scheduledOn = DateTime.Today.AddDays(1);

        context.JobOrders.Add(new JobOrder
        {
            OrderId = orderId,
            OrderNumber = "JO-001",
            OrderTitle = "Test Order",
            OrderType = 1
        });
        context.JobSchedules.Add(new JobSchedule
        {
            ScheduleId = scheduleId,
            OrderId = orderId,
            ScheduledOn = scheduledOn,
            Status = 1,
            Priority = 2,
            MachineNumber = "M1",
            Cancelled = false
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, new NoOpGateway());
        var startOn = DateOnly.FromDateTime(DateTime.Today);

        var result = await controller.GetRange(startOn, 7, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<JobScheduleCalendarItemResponse>>(ok.Value);

        var item = Assert.Single(items);
        Assert.Equal(scheduleId, item.ScheduleId);
        Assert.Equal(orderId, item.OrderId);
        Assert.Equal(scheduledOn, item.StartOn);
        Assert.Equal(1, item.Status);
        Assert.Equal(2, item.Priority);
        Assert.Equal("M1", item.MachineNumber);
    }

    [Fact]
    public async Task GetRange_CancelledSchedule_IsExcluded()
    {
        using var context = CreateContext(nameof(GetRange_CancelledSchedule_IsExcluded));

        var orderId = Guid.NewGuid();
        context.JobOrders.Add(new JobOrder { OrderId = orderId, OrderType = 1 });
        context.JobSchedules.Add(new JobSchedule
        {
            ScheduleId = Guid.NewGuid(),
            OrderId = orderId,
            ScheduledOn = DateTime.Today.AddDays(1),
            Cancelled = true
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, new NoOpGateway());

        var result = await controller.GetRange(DateOnly.FromDateTime(DateTime.Today), 7, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<JobScheduleCalendarItemResponse>>(ok.Value);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetPending_NoData_ReturnsOkEmptyList()
    {
        using var context = CreateContext(nameof(GetPending_NoData_ReturnsOkEmptyList));
        var controller = CreateController(context, new NoOpGateway());

        var result = await controller.GetPending(null, null, null, 100, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<JobSchedulePendingItemResponse>>(ok.Value);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetPending_ReturnsLegacyLikeProjection()
    {
        using var context = CreateContext(nameof(GetPending_ReturnsLegacyLikeProjection));

        var orderId = Guid.NewGuid();
        context.JobOrders.Add(new JobOrder
        {
            OrderId = orderId,
            OrderType = 1,
            OrderNumber = "168312",
            JobNumber = 1,
            CustomerName = "Orbusneich",
            OrderTitle = "Name Card",
            Status = 1,
            OrderedOn = DateTime.Today,
            RequiredOn = DateTime.Today.AddDays(7),
            CreatedOn = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(),
            ModifiedOn = DateTime.UtcNow,
            ModifiedBy = Guid.NewGuid(),
            Retired = false
        });

        context.JobWorkflows.AddRange(
            new JobWorkflow { JobWorkflowId = Guid.NewGuid(), OrderId = orderId, WorkIndex = 0, WorkStatus = 1 },
            new JobWorkflow { JobWorkflowId = Guid.NewGuid(), OrderId = orderId, WorkIndex = 1, WorkStatus = 3 },
            new JobWorkflow { JobWorkflowId = Guid.NewGuid(), OrderId = orderId, WorkIndex = 2, WorkStatus = 2 });

        context.JobSchedules.Add(new JobSchedule
        {
            ScheduleId = Guid.NewGuid(),
            OrderId = orderId,
            ScheduledOn = DateTime.Today,
            UrgencyLevel = 4,
            Cancelled = false
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context, new NoOpGateway());
        var result = await controller.GetPending("168312", null, null, 100, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<JobSchedulePendingItemResponse>>(ok.Value);
        var item = Assert.Single(items);

        Assert.Equal(orderId, item.OrderId);
        Assert.Equal("168312-1", item.OrderNumber);
        Assert.Equal(4, item.UrgencyLevel);
        Assert.Equal(1, item.Step1Status);
        Assert.Equal(3, item.Step2Status);
        Assert.Equal(2, item.Step3Status);
    }

    [Fact]
    public async Task GetPending_StartsWithFilter_WorksForAlphaAndNumericShortcut()
    {
        using var context = CreateContext(nameof(GetPending_StartsWithFilter_WorksForAlphaAndNumericShortcut));

        context.JobOrders.AddRange(
            new JobOrder
            {
                OrderId = Guid.NewGuid(),
                OrderType = 0,
                OrderNumber = "A100",
                JobNumber = 1,
                CustomerName = "Alpha",
                OrderTitle = "First",
                Status = 1,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid(),
                ModifiedOn = DateTime.UtcNow,
                ModifiedBy = Guid.NewGuid(),
                Retired = false
            },
            new JobOrder
            {
                OrderId = Guid.NewGuid(),
                OrderType = 0,
                OrderNumber = "1680",
                JobNumber = 1,
                CustomerName = "Numeric",
                OrderTitle = "Second",
                Status = 1,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid(),
                ModifiedOn = DateTime.UtcNow,
                ModifiedBy = Guid.NewGuid(),
                Retired = false
            });

        await context.SaveChangesAsync();

        var controller = CreateController(context, new NoOpGateway());

        var alpha = await controller.GetPending(null, null, "A", 100, CancellationToken.None);
        var alphaOk = Assert.IsType<OkObjectResult>(alpha.Result);
        var alphaItems = Assert.IsAssignableFrom<IReadOnlyList<JobSchedulePendingItemResponse>>(alphaOk.Value);
        Assert.Single(alphaItems);
        Assert.Equal("A100-1", alphaItems[0].OrderNumber);

        var numeric = await controller.GetPending(null, null, "9", 100, CancellationToken.None);
        var numericOk = Assert.IsType<OkObjectResult>(numeric.Result);
        var numericItems = Assert.IsAssignableFrom<IReadOnlyList<JobSchedulePendingItemResponse>>(numericOk.Value);
        Assert.Single(numericItems);
        Assert.Equal("1680-1", numericItems[0].OrderNumber);
    }

    // -----------------------------------------------------------------------
    // PATCH /{id}/time — validation
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateTime_NullStartOn_ReturnsBadRequest()
    {
        using var context = CreateContext(nameof(UpdateTime_NullStartOn_ReturnsBadRequest));
        var controller = CreateController(context, new NoOpGateway());

        var request = new UpdateJobScheduleTimeRequest { StartOn = null!, EndOn = null };

        var result = await controller.UpdateTime(Guid.NewGuid(), request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task UpdateTime_UnknownId_ReturnsNotFound()
    {
        using var context = CreateContext(nameof(UpdateTime_UnknownId_ReturnsNotFound));
        var controller = CreateController(context, new NoOpGateway());

        var request = new UpdateJobScheduleTimeRequest { StartOn = DateTime.UtcNow, EndOn = null };

        var result = await controller.UpdateTime(Guid.NewGuid(), request, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task UpdateTime_KnownId_ReturnsNoContent()
    {
        var scheduleId = Guid.NewGuid();
        using var context = CreateContext(nameof(UpdateTime_KnownId_ReturnsNoContent));
        var gateway = new StubGateway(scheduleId);
        var controller = CreateController(context, gateway);

        var request = new UpdateJobScheduleTimeRequest
        {
            StartOn = DateTime.UtcNow.AddDays(1),
            EndOn = DateTime.UtcNow.AddDays(2)
        };

        var result = await controller.UpdateTime(scheduleId, request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.True(gateway.UpdateCalled);
        Assert.Equal(1, gateway.LastUpdateRequest!.RescheduledCount);
    }

    [Fact]
    public async Task UpdateTime_KnownId_IncrementsRescheduledCount()
    {
        var scheduleId = Guid.NewGuid();
        // Simulate an existing reschedule count of 3
        using var context = CreateContext(nameof(UpdateTime_KnownId_IncrementsRescheduledCount));
        var gateway = new StubGateway(scheduleId, existingRescheduledCount: 3);
        var controller = CreateController(context, gateway);

        var request = new UpdateJobScheduleTimeRequest { StartOn = DateTime.UtcNow, EndOn = null };

        await controller.UpdateTime(scheduleId, request, CancellationToken.None);

        Assert.Equal(4, gateway.LastUpdateRequest!.RescheduledCount);
    }

    // -----------------------------------------------------------------------
    // PATCH /pending/{orderId}/workflow — workflow step update
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(99)]
    public async Task UpdatePendingWorkflow_InvalidStepIndex_ReturnsBadRequest(int stepIndex)
    {
        var dbName = nameof(UpdatePendingWorkflow_InvalidStepIndex_ReturnsBadRequest) + stepIndex;
        using var readContext = CreateReadContext(dbName);
        using var writeContext = CreateWriteContext(dbName);
        var controller = CreateControllerWithWrite(readContext, writeContext);

        var result = await controller.UpdatePendingWorkflow(Guid.NewGuid(), new UpdatePendingWorkflowRequest
        {
            StepIndex = stepIndex,
            TargetStatus = 1
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public async Task UpdatePendingWorkflow_InvalidTargetStatus_ReturnsBadRequest(int targetStatus)
    {
        var dbName = nameof(UpdatePendingWorkflow_InvalidTargetStatus_ReturnsBadRequest) + targetStatus;
        using var readContext = CreateReadContext(dbName);
        using var writeContext = CreateWriteContext(dbName);
        var controller = CreateControllerWithWrite(readContext, writeContext);

        var result = await controller.UpdatePendingWorkflow(Guid.NewGuid(), new UpdatePendingWorkflowRequest
        {
            StepIndex = 0,
            TargetStatus = targetStatus
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task UpdatePendingWorkflow_UnknownOrder_ReturnsNotFound()
    {
        var dbName = nameof(UpdatePendingWorkflow_UnknownOrder_ReturnsNotFound);
        using var readContext = CreateReadContext(dbName);
        using var writeContext = CreateWriteContext(dbName);
        var controller = CreateControllerWithWrite(readContext, writeContext);

        var result = await controller.UpdatePendingWorkflow(Guid.NewGuid(), new UpdatePendingWorkflowRequest
        {
            StepIndex = 0,
            TargetStatus = 2
        }, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task UpdatePendingWorkflow_ValidRequest_UpdatesStatusAndReturnsNormalizedSteps()
    {
        var dbName = nameof(UpdatePendingWorkflow_ValidRequest_UpdatesStatusAndReturnsNormalizedSteps);
        using var readContext = CreateReadContext(dbName);
        using var writeContext = CreateWriteContext(dbName);

        var orderId = Guid.NewGuid();
        writeContext.JobWorkflows.AddRange(
            new JobWorkflow { JobWorkflowId = Guid.NewGuid(), OrderId = orderId, WorkIndex = 0, WorkStatus = 0 },
            new JobWorkflow { JobWorkflowId = Guid.NewGuid(), OrderId = orderId, WorkIndex = 1, WorkStatus = 1 },
            new JobWorkflow { JobWorkflowId = Guid.NewGuid(), OrderId = orderId, WorkIndex = 2, WorkStatus = 2 });
        await writeContext.SaveChangesAsync();

        var controller = CreateControllerWithWrite(readContext, writeContext);

        var result = await controller.UpdatePendingWorkflow(orderId, new UpdatePendingWorkflowRequest
        {
            StepIndex = 0,
            TargetStatus = 2
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PendingWorkflowUpdateResponse>(ok.Value);
        Assert.Equal(orderId, response.OrderId);
        Assert.Equal(2, response.Step1Status);
        Assert.Equal(1, response.Step2Status);
        Assert.Equal(2, response.Step3Status);
    }

    // -----------------------------------------------------------------------
    // PATCH /pending/{orderId}/urgency — urgency bell update/toggle
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("blue")]
    [InlineData("green")]
    [InlineData("")]
    [InlineData("invalid")]
    public async Task UpdatePendingUrgency_InvalidColor_ReturnsBadRequest(string color)
    {
        var dbName = nameof(UpdatePendingUrgency_InvalidColor_ReturnsBadRequest) + color;
        using var readContext = CreateReadContext(dbName);
        using var writeContext = CreateWriteContext(dbName);
        var controller = CreateControllerWithWrite(readContext, writeContext);

        var result = await controller.UpdatePendingUrgency(Guid.NewGuid(), new UpdatePendingUrgencyRequest
        {
            TargetColor = color
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task UpdatePendingUrgency_UnknownOrder_ReturnsNotFound()
    {
        var dbName = nameof(UpdatePendingUrgency_UnknownOrder_ReturnsNotFound);
        using var readContext = CreateReadContext(dbName);
        using var writeContext = CreateWriteContext(dbName);
        var controller = CreateControllerWithWrite(readContext, writeContext);

        var result = await controller.UpdatePendingUrgency(Guid.NewGuid(), new UpdatePendingUrgencyRequest
        {
            TargetColor = "red"
        }, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task UpdatePendingUrgency_SetRed_ReturnsUrgencyLevelFour()
    {
        var dbName = nameof(UpdatePendingUrgency_SetRed_ReturnsUrgencyLevelFour);
        using var readContext = CreateReadContext(dbName);
        using var writeContext = CreateWriteContext(dbName);

        var orderId = Guid.NewGuid();
        writeContext.JobSchedules.Add(new JobSchedule
        {
            ScheduleId = Guid.NewGuid(),
            OrderId = orderId,
            UrgencyLevel = -1,
            Cancelled = false
        });
        await writeContext.SaveChangesAsync();

        var controller = CreateControllerWithWrite(readContext, writeContext);

        var result = await controller.UpdatePendingUrgency(orderId, new UpdatePendingUrgencyRequest
        {
            TargetColor = "red"
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PendingUrgencyUpdateResponse>(ok.Value);
        Assert.Equal(orderId, response.OrderId);
        Assert.Equal(4, response.UrgencyLevel);
    }

    [Fact]
    public async Task UpdatePendingUrgency_ToggleRedOff_ReturnsNeutral()
    {
        var dbName = nameof(UpdatePendingUrgency_ToggleRedOff_ReturnsNeutral);
        using var readContext = CreateReadContext(dbName);
        using var writeContext = CreateWriteContext(dbName);

        var orderId = Guid.NewGuid();
        writeContext.JobSchedules.Add(new JobSchedule
        {
            ScheduleId = Guid.NewGuid(),
            OrderId = orderId,
            UrgencyLevel = 4,
            Cancelled = false
        });
        await writeContext.SaveChangesAsync();

        var controller = CreateControllerWithWrite(readContext, writeContext);

        var result = await controller.UpdatePendingUrgency(orderId, new UpdatePendingUrgencyRequest
        {
            TargetColor = "red"
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PendingUrgencyUpdateResponse>(ok.Value);
        Assert.Equal(orderId, response.OrderId);
        Assert.Equal(-1, response.UrgencyLevel);
    }

    [Fact]
    public async Task UpdatePendingUrgency_SetYellow_ReturnsUrgencyLevelTwo()
    {
        var dbName = nameof(UpdatePendingUrgency_SetYellow_ReturnsUrgencyLevelTwo);
        using var readContext = CreateReadContext(dbName);
        using var writeContext = CreateWriteContext(dbName);

        var orderId = Guid.NewGuid();
        writeContext.JobSchedules.Add(new JobSchedule
        {
            ScheduleId = Guid.NewGuid(),
            OrderId = orderId,
            UrgencyLevel = -1,
            Cancelled = false
        });
        await writeContext.SaveChangesAsync();

        var controller = CreateControllerWithWrite(readContext, writeContext);

        var result = await controller.UpdatePendingUrgency(orderId, new UpdatePendingUrgencyRequest
        {
            TargetColor = "yellow"
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PendingUrgencyUpdateResponse>(ok.Value);
        Assert.Equal(orderId, response.OrderId);
        Assert.Equal(2, response.UrgencyLevel);
    }

    [Fact]
    public async Task UpdatePendingUrgency_ToggleYellowOff_ReturnsNeutral()
    {
        var dbName = nameof(UpdatePendingUrgency_ToggleYellowOff_ReturnsNeutral);
        using var readContext = CreateReadContext(dbName);
        using var writeContext = CreateWriteContext(dbName);

        var orderId = Guid.NewGuid();
        writeContext.JobSchedules.Add(new JobSchedule
        {
            ScheduleId = Guid.NewGuid(),
            OrderId = orderId,
            UrgencyLevel = 2,
            Cancelled = false
        });
        await writeContext.SaveChangesAsync();

        var controller = CreateControllerWithWrite(readContext, writeContext);

        var result = await controller.UpdatePendingUrgency(orderId, new UpdatePendingUrgencyRequest
        {
            TargetColor = "yellow"
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PendingUrgencyUpdateResponse>(ok.Value);
        Assert.Equal(orderId, response.OrderId);
        Assert.Equal(-1, response.UrgencyLevel);
    }

    // -----------------------------------------------------------------------
    // Test doubles
    // -----------------------------------------------------------------------

    /// <summary>Gateway stub that returns null for SelectAsync (simulates not found).</summary>
    private sealed class NoOpGateway : IJobScheduleStoredProcedureGateway
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

    /// <summary>Gateway stub that returns a precanned record for SelectAsync and captures the UpdateAsync call.</summary>
    private sealed class StubGateway : IJobScheduleStoredProcedureGateway
    {
        private readonly Guid _scheduleId;
        private readonly int? _existingRescheduledCount;

        public bool UpdateCalled { get; private set; }
        public UpdateJobScheduleStoredProcedureRequest? LastUpdateRequest { get; private set; }

        public StubGateway(Guid scheduleId, int? existingRescheduledCount = null)
        {
            _scheduleId = scheduleId;
            _existingRescheduledCount = existingRescheduledCount;
        }

        public Task<JobScheduleStoredProcedureRecord?> SelectAsync(Guid scheduleId, CancellationToken cancellationToken = default)
        {
            if (scheduleId != _scheduleId)
                return Task.FromResult<JobScheduleStoredProcedureRecord?>(null);

            var record = new JobScheduleStoredProcedureRecord(
                ScheduleId: _scheduleId,
                OrderId: Guid.NewGuid(),
                ScheduledOn: DateTime.UtcNow,
                Status: 1,
                Priority: null,
                MachineNumber: null,
                CompletedOn: null,
                ShouldReview: false,
                UrgencyLevel: 0,
                Cancelled: false,
                CancelledOn: null,
                CancelledBy: null,
                RescheduledCount: _existingRescheduledCount,
                RescheduledBy: null,
                RescheduledOn: null);

            return Task.FromResult<JobScheduleStoredProcedureRecord?>(record);
        }

        public Task<Guid> InsertAsync(CreateJobScheduleStoredProcedureRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(Guid.Empty);

        public Task<bool> UpdateAsync(UpdateJobScheduleStoredProcedureRequest request, CancellationToken cancellationToken = default)
        {
            UpdateCalled = true;
            LastUpdateRequest = request;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(Guid scheduleId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}

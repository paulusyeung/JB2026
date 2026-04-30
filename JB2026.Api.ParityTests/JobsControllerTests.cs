using JB2026.Api.Controllers;
using JB2026.Api.Models;
using JB2026.Api.Services;
using JB2026.Api.Options;
using JB2026.EfCore.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JB2026.Api.ParityTests;

public sealed class JobsControllerTests
{
    [Fact]
    public async Task Create_InvalidRequiredOn_ReturnsBadRequest()
    {
        var controller = CreateController(new StubRepository(), CreateCurrentUserProfileService());

        var request = new CreateJobOrderRequest
        {
            OrderNumber = "JB260330",
            JobNumber = "01",
            CustomerName = "Acme",
            CustomerRef = "REF-1",
            OrderTitle = "New order",
            OrderedOn = new DateTime(2026, 3, 30),
            RequiredOn = new DateTime(2026, 3, 29),
            Qty = 100,
            PaymentTerms = "Net 30",
            Remarks = string.Empty,
            Status = 0,
        };

        var result = await controller.Create(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated()
    {
        var repository = new StubRepository();
        var controller = CreateController(repository, CreateCurrentUserProfileService());

        var request = new CreateJobOrderRequest
        {
            OrderNumber = "JB260330",
            JobNumber = "01",
            CustomerName = "Acme",
            CustomerRef = "REF-1",
            OrderTitle = "New order",
            OrderedOn = new DateTime(2026, 3, 30),
            RequiredOn = new DateTime(2026, 4, 1),
            Qty = 100,
            PaymentTerms = "Net 30",
            Remarks = string.Empty,
            Status = 0,
        };

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedResult>(result.Result);
        var payload = Assert.IsType<JobOrderResponse>(created.Value);
        Assert.Equal(request.OrderNumber, payload.OrderNumber);
        Assert.Equal(request.JobNumber, payload.JobNumber);
        Assert.Equal("admin", repository.LastActor);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var controller = CreateController(new StubRepository(), CreateCurrentUserProfileService());

        var result = await controller.Update(Guid.NewGuid(), new UpdateJobOrderRequest
        {
            CustomerName = "Acme",
            CustomerRef = "REF-2",
            OrderTitle = "Updated order",
            RequiredOn = new DateTime(2026, 4, 5),
            Qty = 50,
            PaymentTerms = "Net 14",
            Remarks = string.Empty,
            Status = 1,
        });

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task Update_KnownId_ReturnsOk()
    {
        var repository = new StubRepository();
        var controller = CreateController(repository, CreateCurrentUserProfileService());
        var orderId = repository.SeedOrderId;

        var result = await controller.Update(orderId, new UpdateJobOrderRequest
        {
            CustomerName = "Acme 2",
            CustomerRef = "REF-2",
            OrderTitle = "Updated order",
            RequiredOn = new DateTime(2026, 4, 5),
            Qty = 50,
            PaymentTerms = "Net 14",
            Remarks = "Updated",
            Status = 1,
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<JobOrderResponse>(ok.Value);
        Assert.Equal(orderId, payload.OrderId);
        Assert.Equal("Acme 2", payload.CustomerName);
        Assert.Equal("admin", repository.LastActor);
    }

    private static JobsController CreateController(IJobManagementRepository repository, ICurrentUserProfileService currentUserProfileService)
    {
        var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>()
                .UseInMemoryDatabase($"jobs-controller-tests-{Guid.NewGuid():N}")
                .Options);

        var controller = new JobsController(
            repository,
            new StubJobAttachmentStoredProcedureGateway(),
            readContext,
            currentUserProfileService,
            NullLogger<JobsController>.Instance,
            Options.Create(new LegacyFilesOptions()));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    private static ICurrentUserProfileService CreateCurrentUserProfileService()
    {
        return new StubCurrentUserProfileService();
    }

    private sealed class StubCurrentUserProfileService : ICurrentUserProfileService
    {
        public UserProfileResponse? GetCurrentUser()
        {
            return new UserProfileResponse
            {
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Username = "admin",
                DisplayName = "Administrator",
                Role = "Admin"
            };
        }
    }

    private sealed class StubRepository : IJobManagementRepository
    {
        public Guid SeedOrderId { get; } = Guid.NewGuid();

        public string? LastActor { get; private set; }

        public IReadOnlyList<JobListItemResponse> GetRange(DateOnly startOn, int days) => [];

        public JobDetailResponse? GetJobDetail(Guid orderId) => null;

        public IReadOnlyList<string> GetStyleTitles(Guid orderId) => [];

        public IReadOnlyList<JobOrderResponse> GetJobOrders(int take) => [];

        public IReadOnlyList<JobOrderResponse> GetJobList(string? lookup, int commonQuery, string? startsWith, int take, DateOnly? startOn = null, DateOnly? endOn = null) => [];

        public IReadOnlyList<JobOrderResponse> GetOrderList(string? lookup, int commonQuery, string? startsWith, int take, DateOnly? startOn = null, DateOnly? endOn = null) => [];

        public IReadOnlyList<JobStatsResponse> GetJobStats(DateOnly? startOn, DateOnly? endOn) => [];

        public JobOrderResponse? GetJobOrder(Guid orderId) => null;

        public Task<JobOrderResponse> CreateJobOrder(CreateJobOrderRequest request, string actor)
        {
            LastActor = actor;
            return Task.FromResult(new JobOrderResponse
            {
                OrderId = Guid.NewGuid(),
                OrderType = 0,
                OrderNumber = request.OrderNumber,
                JobNumber = request.JobNumber,
                CustomerName = request.CustomerName,
                CustomerRef = request.CustomerRef,
                OrderTitle = request.OrderTitle,
                ProductCode = string.Empty,
                ProductStyle = string.Empty,
                OutputRef = string.Empty,
                InvoiceRef = string.Empty,
                InvoiceAmount = 0m,
                AttachmentProductCount = 0,
                AttachmentCustomerCount = 0,
                OrderedBy = actor,
                OrderedOn = request.OrderedOn,
                RequiredOn = request.RequiredOn,
                CompletedOn = null,
                Qty = request.Qty,
                PaymentTerms = request.PaymentTerms,
                Remarks = request.Remarks,
                Status = request.Status,
                CreatedBy = actor,
                CreatedOn = DateTime.UtcNow,
                ModifiedBy = actor,
                ModifiedOn = DateTime.UtcNow,
            });
        }

        public Task<JobOrderResponse?> UpdateJobOrder(Guid orderId, UpdateJobOrderRequest request, string actor)
        {
            LastActor = actor;
            if (orderId != SeedOrderId)
            {
                return Task.FromResult<JobOrderResponse?>(null);
            }

            return Task.FromResult<JobOrderResponse?>(new JobOrderResponse
            {
                OrderId = orderId,
                OrderType = 0,
                OrderNumber = "JB260330",
                JobNumber = "01",
                CustomerName = request.CustomerName,
                CustomerRef = request.CustomerRef,
                OrderTitle = request.OrderTitle,
                ProductCode = string.Empty,
                ProductStyle = string.Empty,
                OutputRef = string.Empty,
                InvoiceRef = string.Empty,
                InvoiceAmount = 0m,
                AttachmentProductCount = 0,
                AttachmentCustomerCount = 0,
                OrderedBy = actor,
                OrderedOn = new DateTime(2026, 3, 30),
                RequiredOn = request.RequiredOn,
                CompletedOn = null,
                Qty = request.Qty,
                PaymentTerms = request.PaymentTerms,
                Remarks = request.Remarks,
                Status = request.Status,
                CreatedBy = actor,
                CreatedOn = new DateTime(2026, 3, 30),
                ModifiedBy = actor,
                ModifiedOn = DateTime.UtcNow,
            });
        }

        public Task<JobOrderResponse?> DeleteJobOrder(Guid orderId)
            => Task.FromResult<JobOrderResponse?>(null);
    }

    private sealed class StubJobAttachmentStoredProcedureGateway : IJobAttachmentStoredProcedureGateway
    {
        public Task<JobAttachmentStoredProcedureRecord?> SelectAsync(Guid attachmentId, CancellationToken cancellationToken = default)
            => Task.FromResult<JobAttachmentStoredProcedureRecord?>(null);

        public Task<Guid> InsertAsync(CreateJobAttachmentStoredProcedureRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(Guid.NewGuid());

        public Task<bool> UpdateAsync(UpdateJobAttachmentStoredProcedureRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<bool> DeleteAsync(Guid attachmentId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}
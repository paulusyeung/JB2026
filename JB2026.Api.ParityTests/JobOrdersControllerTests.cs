using JB2026.Api.Controllers;
using JB2026.Api.Models;
using JB2026.Api.Options;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace JB2026.Api.ParityTests;

public sealed class JobOrdersControllerTests
{
    [Fact]
    public void GetAll_JobListMode_UsesJobListRepositoryPath()
    {
        var repository = new StubRepository();
        var controller = CreateController(repository);

        var result = controller.GetAll(null, "acme", 2, "A", "job");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<JobOrderResponse>>(ok.Value);
        Assert.Single(payload);
        Assert.True(repository.JobListCalled);
        Assert.False(repository.OrderListCalled);
        Assert.False(repository.JobOrdersCalled);
    }

    [Fact]
    public void GetAll_FilteredOrderList_UsesOrderListRepositoryPath()
    {
        var repository = new StubRepository();
        var controller = CreateController(repository);

        var result = controller.GetAll(null, "acme", 0, null, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<JobOrderResponse>>(ok.Value);
        Assert.Single(payload);
        Assert.False(repository.JobListCalled);
        Assert.True(repository.OrderListCalled);
        Assert.False(repository.JobOrdersCalled);
    }

    [Fact]
    public void GetStats_InvalidTake_ReturnsBadRequest()
    {
        var repository = new StubRepository();
        var controller = CreateController(repository);

        var result = controller.GetStats(null, null, 0);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public void GetStats_ValidRequest_UsesRepositoryPath()
    {
        var repository = new StubRepository();
        var controller = CreateController(repository);

        var result = controller.GetStats(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<JobStatsResponse>>(ok.Value);
        Assert.Single(payload);
        Assert.True(repository.JobStatsCalled);
    }

    private static JobOrdersController CreateController(IJobManagementRepository repository)
    {
        var controller = new JobOrdersController(
            repository,
            new StubCurrentUserProfileService(),
            Microsoft.Extensions.Options.Options.Create(new JobListOptions()),
            NullLogger<JobOrdersController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
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
        public bool JobListCalled { get; private set; }

        public bool OrderListCalled { get; private set; }

        public bool JobOrdersCalled { get; private set; }

        public bool JobStatsCalled { get; private set; }

        public IReadOnlyList<JobListItemResponse> GetRange(DateOnly startOn, int days) => [];

        public JobDetailResponse? GetJobDetail(Guid orderId) => null;

        public IReadOnlyList<string> GetStyleTitles(Guid orderId) => [];

        public IReadOnlyList<JobOrderResponse> GetJobOrders(int take)
        {
            JobOrdersCalled = true;
            return [CreateResponse("job-orders")];
        }

        public IReadOnlyList<JobOrderResponse> GetJobList(string? lookup, int commonQuery, string? startsWith, int take, DateOnly? startOn = null, DateOnly? endOn = null)
        {
            JobListCalled = true;
            return [CreateResponse("job-list")];
        }

        public IReadOnlyList<JobOrderResponse> GetOrderList(string? lookup, int commonQuery, string? startsWith, DateOnly? startOn = null, DateOnly? endOn = null)
        {
            OrderListCalled = true;
            return [CreateResponse("order-list")];
        }

        public IReadOnlyList<JobStatsResponse> GetJobStats(DateOnly? startOn, DateOnly? endOn)
        {
            JobStatsCalled = true;
            return
            [
                new JobStatsResponse
                {
                    JobNumber = "JB260301-01",
                    CustomerName = "Acme",
                    Brand = "Sample",
                    PurchaseOrder = "PO-1",
                    SalesRep = "admin",
                    GrossProfit = 0.25m,
                    Cost = 75m,
                    InvoiceAmount = 100m,
                    InvNumber = "INV-100",
                    InvDate = new DateTime(2026, 4, 1),
                    Year = 2026,
                    Month = 4,
                }
            ];
        }

        public JobOrderResponse? GetJobOrder(Guid orderId) => null;

        public Task<JobOrderResponse> CreateJobOrder(CreateJobOrderRequest request, string actor)
            => Task.FromResult(CreateResponse(request.OrderNumber));

        public Task<JobOrderResponse?> UpdateJobOrder(Guid orderId, UpdateJobOrderRequest request, string actor)
            => Task.FromResult<JobOrderResponse?>(CreateResponse(request.OrderTitle));

        public Task<JobOrderResponse?> DeleteJobOrder(Guid orderId)
            => Task.FromResult<JobOrderResponse?>(CreateResponse(orderId.ToString()));

        private static JobOrderResponse CreateResponse(string orderNumber)
        {
            return new JobOrderResponse
            {
                OrderId = Guid.NewGuid(),
                OrderType = 0,
                OrderNumber = orderNumber,
                JobNumber = "01",
                CustomerName = "Acme",
                CustomerRef = "REF-1",
                OrderTitle = "Sample",
                ProductCode = string.Empty,
                ProductStyle = string.Empty,
                OutputRef = string.Empty,
                InvoiceRef = string.Empty,
                InvoiceAmount = 0m,
                AttachmentProductCount = 0,
                AttachmentCustomerCount = 0,
                OrderedBy = "admin",
                OrderedOn = new DateTime(2026, 4, 1),
                RequiredOn = new DateTime(2026, 4, 3),
                CompletedOn = null,
                Qty = 1m,
                PaymentTerms = "Net 30",
                Remarks = string.Empty,
                Status = 1,
                CreatedBy = "admin",
                CreatedOn = new DateTime(2026, 4, 1),
                ModifiedBy = "admin",
                ModifiedOn = new DateTime(2026, 4, 1)
            };
        }
    }
}
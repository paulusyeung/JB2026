using JB2026.Api.Controllers;
using JB2026.Api.Models;
using JB2026.Api.Options;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using UglyToad.PdfPig;

namespace JB2026.Api.ParityTests;

public sealed class JobOrderPrintControllerTests
{
    private static JB5LegacyReadContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<JB5LegacyReadContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new JB5LegacyReadContext(options);
    }

    private static JobsController CreateController(JB5LegacyReadContext context)
    {
        var repository = new StubJobManagementRepository();
        var composer = new JobOrderPrintComposer(context, Microsoft.Extensions.Options.Options.Create(new LegacyFilesOptions()));
        var renderer = new JobOrderPdfRenderer();
        var controller = new JobsController(
            repository,
            new StubJobAttachmentStoredProcedureGateway(),
            context,
            new StubCurrentUserProfileService(),
            NullLogger<JobsController>.Instance,
            Microsoft.Extensions.Options.Options.Create(new LegacyFilesOptions()),
            composer,
            renderer)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }

    private static JobOrder SeedOrder(JB5LegacyReadContext context, string orderNumber, int? jobNumber = null, string? customerName = null)
    {
        var order = new JobOrder
        {
            OrderId = Guid.NewGuid(),
            OrderType = 1,
            OrderNumber = orderNumber,
            JobNumber = jobNumber,
            CustomerName = customerName ?? "Test Customer",
            CustomerRef = "CREF-001",
            OrderTitle = "Test Order Title",
            ProductCode = "PC-001",
            OrderedOn = new DateTime(2026, 1, 10),
            RequiredOn = new DateTime(2026, 3, 31),
            Status = 1,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(),
            ModifiedOn = DateTime.UtcNow,
            ModifiedBy = Guid.NewGuid(),
            Retired = false
        };

        context.JobOrders.Add(order);
        context.SaveChanges();

        return order;
    }

    private static JobWorkflow SeedWorkflow(JB5LegacyReadContext context, Guid orderId, int workIndex, string title, string? instruction = null)
    {
        var workflow = new JobWorkflow
        {
            JobWorkflowId = Guid.NewGuid(),
            OrderId = orderId,
            WorkIndex = workIndex,
            WorkTitle = title,
            WorkInstruction = instruction ?? $"Instructions for {title}",
            WorkStatus = 0
        };

        context.JobWorkflows.Add(workflow);
        context.SaveChanges();

        return workflow;
    }

    private static string ExtractText(byte[] pdfBytes)
    {
        using var document = PdfDocument.Open(pdfBytes);
        var pages = document.GetPages();
        return string.Concat(pages.SelectMany(p => p.GetWords()).Select(w => w.Text + " "));
    }

    [Fact]
    public async Task PrintJobOrder_ReturnsPdfFile_ForExistingOrder()
    {
        using var context = CreateContext(nameof(PrintJobOrder_ReturnsPdfFile_ForExistingOrder));
        var order = SeedOrder(context, "JO-2026", 1);

        var controller = CreateController(context);
        var request = new JobOrderPrintRequest
        {
            Layout = "default",
            NoPicture = false,
            NoProductDetails = false,
            SelectedWorkflowIndices = Array.Empty<int>()
        };

        var result = await controller.PrintJobOrder(order.OrderId, request, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Contains("JO-2026-01.pdf", file.FileDownloadName);
        Assert.NotEmpty(file.FileContents);
    }

    [Fact]
    public async Task PrintJobOrder_ReturnsNotFound_WhenOrderMissing()
    {
        using var context = CreateContext(nameof(PrintJobOrder_ReturnsNotFound_WhenOrderMissing));
        var controller = CreateController(context);

        var request = new JobOrderPrintRequest();
        var result = await controller.PrintJobOrder(Guid.NewGuid(), request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PrintJobOrder_IncludesHeaderFields_InPdfContent()
    {
        using var context = CreateContext(nameof(PrintJobOrder_IncludesHeaderFields_InPdfContent));
        var order = SeedOrder(context, "JO-HDR", null, "Acme Corp");

        var controller = CreateController(context);
        var request = new JobOrderPrintRequest();
        var result = await controller.PrintJobOrder(order.OrderId, request, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        var content = ExtractText(file.FileContents);

        Assert.Contains("JO-HDR", content);
        Assert.Contains("Acme Corp", content);
        Assert.Contains("Order Number", content);
        Assert.Contains("Customer Name", content);
    }

    [Fact]
    public async Task PrintJobOrder_FiltersWorkflows_BySelectedIndices()
    {
        using var context = CreateContext(nameof(PrintJobOrder_FiltersWorkflows_BySelectedIndices));
        var order = SeedOrder(context, "JO-FILTER");
        SeedWorkflow(context, order.OrderId, 1, "Cutting", "Cut fabric");
        SeedWorkflow(context, order.OrderId, 2, "Sewing", "Sew pieces");
        SeedWorkflow(context, order.OrderId, 3, "Packing", "Pack items");

        var controller = CreateController(context);
        var request = new JobOrderPrintRequest
        {
            SelectedWorkflowIndices = [1, 3]
        };

        var result = await controller.PrintJobOrder(order.OrderId, request, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        var content = ExtractText(file.FileContents);

        Assert.Contains("Cutting", content);
        Assert.Contains("Packing", content);
        Assert.DoesNotContain("Sewing", content);
    }

    [Fact]
    public async Task PrintJobOrder_IncludesAllWorkflows_WhenNoIndicesSelected()
    {
        using var context = CreateContext(nameof(PrintJobOrder_IncludesAllWorkflows_WhenNoIndicesSelected));
        var order = SeedOrder(context, "JO-ALL");
        SeedWorkflow(context, order.OrderId, 1, "Alpha");
        SeedWorkflow(context, order.OrderId, 2, "Beta");

        var controller = CreateController(context);
        var request = new JobOrderPrintRequest
        {
            SelectedWorkflowIndices = Array.Empty<int>()
        };

        var result = await controller.PrintJobOrder(order.OrderId, request, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        var content = ExtractText(file.FileContents);

        Assert.Contains("Alpha", content);
        Assert.Contains("Beta", content);
    }

    [Fact]
    public async Task PrintJobOrder_OmitsProductDetailsSection_WhenFlagSet()
    {
        using var context = CreateContext(nameof(PrintJobOrder_OmitsProductDetailsSection_WhenFlagSet));
        var order = SeedOrder(context, "JO-NOPRD");
        order.ProductStyle = "Style A";
        order.ProductDetails = "Unique-Detail-XYZ";
        context.SaveChanges();

        var controller = CreateController(context);
        var request = new JobOrderPrintRequest { NoProductDetails = true };

        var result = await controller.PrintJobOrder(order.OrderId, request, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        var content = ExtractText(file.FileContents);

        Assert.DoesNotContain("Unique-Detail-XYZ", content);
    }

    [Fact]
    public async Task PrintJobOrder_PreservesCjkText_InPdfContent()
    {
        using var context = CreateContext(nameof(PrintJobOrder_PreservesCjkText_InPdfContent));
        var order = SeedOrder(context, "JO-CJK", null, "客戶名稱");

        var controller = CreateController(context);
        var request = new JobOrderPrintRequest();

        var result = await controller.PrintJobOrder(order.OrderId, request, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        var content = ExtractText(file.FileContents);

        Assert.Contains("客戶名稱", content);
    }

    private sealed class StubJobManagementRepository : IJobManagementRepository
    {
        public IReadOnlyList<JobListItemResponse> GetRange(DateOnly startOn, int days) => [];
        public JobDetailResponse? GetJobDetail(Guid id) => null;
        public IReadOnlyList<string> GetStyleTitles(Guid orderId) => [];
        public IReadOnlyList<JobOrderResponse> GetJobOrders(int take) => [];
        public IReadOnlyList<JobOrderResponse> GetJobList(string? lookup, int commonQuery, string? startsWith, int take, DateOnly? startOn = null, DateOnly? endOn = null, int? status = null) => [];
        public IReadOnlyList<JobOrderResponse> GetOrderList(string? lookup, int commonQuery, string? startsWith, int take, DateOnly? startOn = null, DateOnly? endOn = null) => [];
        public IReadOnlyList<JobStatsResponse> GetJobStats(DateOnly? startOn, DateOnly? endOn) => [];
        public JobOrderResponse? GetJobOrder(Guid orderId) => null;
        public Task<JobOrderResponse> CreateJobOrder(CreateJobOrderRequest request, string actor) => throw new NotImplementedException();
        public Task<JobOrderResponse?> UpdateJobOrder(Guid orderId, UpdateJobOrderRequest request, string actor) => throw new NotImplementedException();
        public Task<JobOrderResponse?> DeleteJobOrder(Guid orderId) => Task.FromResult<JobOrderResponse?>(null);
    }

    private sealed class StubCurrentUserProfileService : ICurrentUserProfileService
    {
        public UserProfileResponse? GetCurrentUser() => null;
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

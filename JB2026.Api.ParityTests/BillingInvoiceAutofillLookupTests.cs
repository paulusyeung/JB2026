using JB2026.Api.Models.Billing;
using JB2026.Api.Options;
using JB2026.Api.Services.Billing;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace JB2026.Api.ParityTests;

public sealed class BillingInvoiceAutofillLookupTests
{
    [Fact]
    public void ParseCanonicalJobNumberExpression_ExpandsSlashSeparatedSegmentInOrder()
    {
        var result = BillingInvoiceAutofillHelper.ParseCanonicalJobNumberExpression("168824-1/2/3");

        Assert.Equal(["168824-1", "168824-2", "168824-3"], result);
    }

    [Fact]
    public void ParseCanonicalJobNumberExpression_ExpandsCommaSeparatedSegmentsInOrder()
    {
        var result = BillingInvoiceAutofillHelper.ParseCanonicalJobNumberExpression("168824-2, 168825-1, 168824-2");

        Assert.Equal(["168824-2", "168825-1"], result);
    }

    [Fact]
    public void ParseCanonicalJobNumberExpression_InvalidSegment_ReturnsEmpty()
    {
        var result = BillingInvoiceAutofillHelper.ParseCanonicalJobNumberExpression("168824-1, bad-value");

        Assert.Empty(result);
    }

    [Fact]
    public void ExtractSectionOneDescription_PlainText_ReturnsSectionBodyWithoutHeader()
    {
        const string productDetails = "1.印刷內容：\nLine A\nLine B\n2.尺寸：\nIgnore me";

        var description = BillingInvoiceAutofillHelper.ExtractSectionOneDescription(productDetails);

        Assert.Equal("Line A\nLine B", description);
    }

    [Fact]
    public void ExtractSectionOneDescription_HtmlRichText_NormalizesMarkup()
    {
        const string productDetails = "<p>1.印刷內容：</p><div>Line A</div><div>Line B</div><p>2.尺寸：</p><div>Ignore me</div>";

        var description = BillingInvoiceAutofillHelper.ExtractSectionOneDescription(productDetails);

        Assert.Equal("Line A\nLine B", description);
    }

    [Fact]
    public void ExtractSectionOneDescription_MissingSectionOne_ReturnsNull()
    {
        const string productDetails = "2.尺寸：\nIgnore me";

        var description = BillingInvoiceAutofillHelper.ExtractSectionOneDescription(productDetails);

        Assert.Null(description);
    }

    [Fact]
    public void ExtractSectionOneDescription_MixedFormatting_StopsAtNextNumberedSection()
    {
        const string productDetails = "<div>1.印刷內容：</div><div> First line </div><br><div>Second line</div><div>3.加工：</div><div>Ignore me</div>";

        var description = BillingInvoiceAutofillHelper.ExtractSectionOneDescription(productDetails);

        Assert.Equal(" First line\nSecond line", description);
    }

    [Fact]
    public void ExtractSectionOneDescription_RemovesSpacerLinesBetweenContentRows()
    {
        const string productDetails = "1.印刷內容：\nLine A\n\nLine B\n\n\nLine C\n2.尺寸：\nIgnore me";

        var description = BillingInvoiceAutofillHelper.ExtractSectionOneDescription(productDetails);

        Assert.Equal("Line A\nLine B\nLine C", description);
    }

    [Fact]
    public async Task LookupInvoiceEditorAutofillAsync_ReturnsResolvedUnresolvedManualReviewAndDeduplicatedRows()
    {
        await using var readContext = CreateReadContext();
        SeedJobs(readContext);

        var service = CreateBillingService(readContext);

        var results = await service.LookupInvoiceEditorAutofillAsync([
            "168824-1",
            "168824-2",
            "168825-1",
            "168824-1",
            "999999-1"
        ]);

        Assert.Collection(results,
            first =>
            {
                Assert.Equal("168824-1", first.CanonicalJobNumber);
                Assert.Equal(InvoiceEditorAutofillLookupStatuses.Resolved, first.Status);
                Assert.Equal("PO-001", first.PurchaseOrder);
                Assert.Equal("Line A\nLine B", first.Description);
            },
            second =>
            {
                Assert.Equal("168824-2", second.CanonicalJobNumber);
                Assert.Equal(InvoiceEditorAutofillLookupStatuses.Resolved, second.Status);
                Assert.Equal("PO-002", second.PurchaseOrder);
                Assert.Equal("HTML A\nHTML B", second.Description);
            },
            third =>
            {
                Assert.Equal("168825-1", third.CanonicalJobNumber);
                Assert.Equal(InvoiceEditorAutofillLookupStatuses.ResolvedButMissingSection1, third.Status);
                Assert.Equal("PO-003", third.PurchaseOrder);
                Assert.Equal(string.Empty, third.Description);
                Assert.Contains("Manual review", third.Message, StringComparison.OrdinalIgnoreCase);
            },
            fourth =>
            {
                Assert.Equal("999999-1", fourth.CanonicalJobNumber);
                Assert.Equal(InvoiceEditorAutofillLookupStatuses.Unresolved, fourth.Status);
            });
    }

    [Fact]
    public async Task LookupInvoiceEditorAutofillAsync_MatchesCanonicalSuffixAgainstStoredNumericJobNumber()
    {
        await using var readContext = CreateReadContext();
        readContext.JobOrders.Add(new JobOrder
        {
            OrderId = Guid.NewGuid(),
            OrderType = 1,
            OrderNumber = "168900",
            JobNumber = 1,
            PONumber = "PO-900",
            ProductDetails = "1.印刷內容：\nZero padded logical match",
            Status = 1,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(),
            ModifiedOn = DateTime.UtcNow,
            ModifiedBy = Guid.NewGuid(),
            Retired = false,
        });
        await readContext.SaveChangesAsync();

        var service = CreateBillingService(readContext);
        var results = await service.LookupInvoiceEditorAutofillAsync(["168900-1"]);

        var result = Assert.Single(results);
        Assert.Equal(InvoiceEditorAutofillLookupStatuses.Resolved, result.Status);
        Assert.Equal("PO-900", result.PurchaseOrder);
        Assert.Equal("Zero padded logical match", result.Description);
    }

    [Fact]
    public async Task LookupInvoiceEditorAutofillAsync_NormalizesStoredOrderNumberPaddingAndCase()
    {
        await using var readContext = CreateReadContext();
        readContext.JobOrders.Add(new JobOrder
        {
            OrderId = Guid.NewGuid(),
            OrderType = 1,
            OrderNumber = " abC123  ",
            JobNumber = 2,
            PONumber = "PO-123",
            ProductDetails = "1.印刷內容：\nNormalized match",
            Status = 1,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(),
            ModifiedOn = DateTime.UtcNow,
            ModifiedBy = Guid.NewGuid(),
            Retired = false,
        });
        await readContext.SaveChangesAsync();

        var service = CreateBillingService(readContext);
        var results = await service.LookupInvoiceEditorAutofillAsync(["ABC123-2"]);

        var result = Assert.Single(results);
        Assert.Equal(InvoiceEditorAutofillLookupStatuses.Resolved, result.Status);
        Assert.Equal("PO-123", result.PurchaseOrder);
        Assert.Equal("Normalized match", result.Description);
    }

    [Fact]
    public async Task LookupInvoiceEditorAutofillAsync_FallsBackToOriginalPurchaseOrder()
    {
        await using var readContext = CreateReadContext();
        readContext.JobOrders.Add(new JobOrder
        {
            OrderId = Guid.NewGuid(),
            OrderType = 1,
            OrderNumber = "168902",
            JobNumber = 1,
            PONumber = null,
            OriginalPONumber = "PO-ORIGINAL-902",
            ProductDetails = "1.印刷內容：\nOriginal PO fallback",
            Status = 1,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(),
            ModifiedOn = DateTime.UtcNow,
            ModifiedBy = Guid.NewGuid(),
            Retired = false,
        });
        await readContext.SaveChangesAsync();

        var service = CreateBillingService(readContext);
        var results = await service.LookupInvoiceEditorAutofillAsync(["168902-1"]);

        var result = Assert.Single(results);
        Assert.Equal("PO-ORIGINAL-902", result.PurchaseOrder);
        Assert.Equal("Original PO fallback", result.Description);
    }

    [Fact]
    public async Task LookupInvoiceEditorAutofillAsync_PrefersCustomerRefForPurchaseOrder()
    {
        await using var readContext = CreateReadContext();
        readContext.JobOrders.Add(new JobOrder
        {
            OrderId = Guid.NewGuid(),
            OrderType = 1,
            OrderNumber = "168903",
            JobNumber = 1,
            CustomerRef = "PO-CUSTOMER-903",
            PONumber = "PO-FIELD-903",
            OriginalPONumber = "PO-ORIGINAL-903",
            ProductDetails = "1.印刷內容：\nCustomer ref wins",
            Status = 1,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(),
            ModifiedOn = DateTime.UtcNow,
            ModifiedBy = Guid.NewGuid(),
            Retired = false,
        });
        await readContext.SaveChangesAsync();

        var service = CreateBillingService(readContext);
        var results = await service.LookupInvoiceEditorAutofillAsync(["168903-1"]);

        var result = Assert.Single(results);
        Assert.Equal("PO-CUSTOMER-903", result.PurchaseOrder);
        Assert.Equal("Customer ref wins", result.Description);
    }

    [Fact]
    public async Task LookupInvoiceEditorAutofillAsync_SanitizesMalformedLegacyTextForJsonSerialization()
    {
        await using var readContext = CreateReadContext();
        readContext.JobOrders.Add(new JobOrder
        {
            OrderId = Guid.NewGuid(),
            OrderType = 1,
            OrderNumber = "168901",
            JobNumber = 1,
            PONumber = "PO-\uD800",
            ProductDetails = "1.印刷內容：\nBad \uD800 details",
            Status = 1,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(),
            ModifiedOn = DateTime.UtcNow,
            ModifiedBy = Guid.NewGuid(),
            Retired = false,
        });
        await readContext.SaveChangesAsync();

        var service = CreateBillingService(readContext);
        var results = await service.LookupInvoiceEditorAutofillAsync(["168901-1"]);

        var result = Assert.Single(results);
        var json = JsonSerializer.Serialize(result);

        Assert.Contains("\uFFFD", result.PurchaseOrder);
        Assert.Contains("\uFFFD", result.ProductDetails);
        Assert.Contains("\uFFFD", result.Description);
        Assert.Contains("\\ufffd", json, StringComparison.OrdinalIgnoreCase);
    }

    private static BillingService CreateBillingService(JB5LegacyReadContext readContext)
    {
        var services = new ServiceCollection();
        services.AddSingleton(readContext);
        var provider = services.BuildServiceProvider();

        return new BillingService(
            new StubInvoiceNinjaHttpClient(),
            Microsoft.Extensions.Options.Options.Create(new BillingOptions()),
            provider,
            NullLogger<BillingService>.Instance);
    }

    private static JB5LegacyReadContext CreateReadContext()
    {
        var options = new DbContextOptionsBuilder<JB5LegacyReadContext>()
            .UseInMemoryDatabase($"billing-autofill-{Guid.NewGuid():N}")
            .Options;
        return new JB5LegacyReadContext(options);
    }

    private static void SeedJobs(JB5LegacyReadContext readContext)
    {
        readContext.JobOrders.AddRange(
            new JobOrder
            {
                OrderId = Guid.NewGuid(),
                OrderType = 1,
                OrderNumber = "168824",
                JobNumber = 1,
                PONumber = "PO-001",
                ProductDetails = "1.印刷內容：\nLine A\nLine B\n2.尺寸：\nIgnore me",
                Status = 1,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid(),
                ModifiedOn = DateTime.UtcNow,
                ModifiedBy = Guid.NewGuid(),
                Retired = false,
            },
            new JobOrder
            {
                OrderId = Guid.NewGuid(),
                OrderType = 1,
                OrderNumber = "168824",
                JobNumber = 2,
                PONumber = "PO-002",
                ProductDetails = "<p>1.印刷內容：</p><div>HTML A</div><div>HTML B</div><p>2.尺寸：</p><div>Ignore me</div>",
                Status = 1,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid(),
                ModifiedOn = DateTime.UtcNow,
                ModifiedBy = Guid.NewGuid(),
                Retired = false,
            },
            new JobOrder
            {
                OrderId = Guid.NewGuid(),
                OrderType = 1,
                OrderNumber = "168825",
                JobNumber = 1,
                PONumber = "PO-003",
                ProductDetails = "2.尺寸：\nIgnore me",
                Status = 1,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid(),
                ModifiedOn = DateTime.UtcNow,
                ModifiedBy = Guid.NewGuid(),
                Retired = false,
            });

        readContext.SaveChanges();
    }

    private sealed class StubInvoiceNinjaHttpClient : IInvoiceNinjaHttpClient
    {
        public Task<T?> GetAsync<T>(string endpoint) where T : class => Task.FromResult<T?>(null);

        public Task<T> PostAsync<T>(string endpoint, object body) where T : class => throw new NotSupportedException();

        public Task<InvoiceNinjaBinaryResponse> PostStreamAsync(string endpoint, object body) => throw new NotSupportedException();

        public Task<T> PutAsync<T>(string endpoint, object body) where T : class => throw new NotSupportedException();

        public Task<bool> IsConnectedAsync() => Task.FromResult(true);

        public (bool isValid, string errorMessage) ValidateConfiguration() => (true, string.Empty);

        public Task<byte[]?> GetStreamAsync(string endpoint) => Task.FromResult<byte[]?>(null);
    }
}
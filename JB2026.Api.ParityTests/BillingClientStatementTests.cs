using JB2026.Api.Controllers;
using JB2026.Api.Models;
using JB2026.Api.Models.Billing;
using JB2026.Api.Options;
using JB2026.Api.Services;
using JB2026.Api.Services.Billing;
using JB2026.EfCore.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace JB2026.Api.ParityTests;

public sealed class BillingClientStatementTests
{
    [Fact]
    public async Task PrepareClientStatementLaunchAsync_RejectsMissingClientSelection()
    {
        await using var readContext = CreateReadContext();
        var service = CreateBillingService(readContext, new RecordingInvoiceNinjaHttpClient());

        var exception = await Assert.ThrowsAsync<BillingException>(() =>
            service.PrepareClientStatementLaunchAsync(new BillingStatementLaunchRequest()));

        Assert.Equal("INVALID_REQUEST", exception.ErrorCode);
        Assert.Equal("Client selection is required.", exception.Message);
    }

    [Fact]
    public async Task GetClientStatementAsync_RejectsUnsupportedStatusOption()
    {
        await using var readContext = CreateReadContext();
        var client = new InvoiceNinjaClientResponse { Id = "client-1", Name = "Acme" };
        var service = CreateBillingService(readContext, new RecordingInvoiceNinjaHttpClient(client));

        var exception = await Assert.ThrowsAsync<BillingException>(() =>
            service.GetClientStatementAsync(new BillingStatementLaunchRequest
            {
                ExternalClientId = "client-1",
                DateRangePreset = BillingStatementDateRangePresets.AllOutstanding,
                Status = BillingStatementStatuses.Paid,
            }));

        Assert.Equal("INVALID_REQUEST", exception.ErrorCode);
        Assert.Equal("The selected status option is not currently supported for statement generation.", exception.Message);
    }

    [Fact]
    public async Task GetClientStatementAsync_MapsRequestToInvoiceNinjaClientStatementPayload()
    {
        await using var readContext = CreateReadContext();
        var client = new InvoiceNinjaClientResponse { Id = "client-1", Name = "Acme" };
        var httpClient = new RecordingInvoiceNinjaHttpClient(client)
        {
            PostStreamResponse = new InvoiceNinjaBinaryResponse
            {
                Content = [1, 2, 3],
                ContentType = "application/pdf",
                FileName = "statement.pdf"
            }
        };

        var service = CreateBillingService(readContext, httpClient);

        var document = await service.GetClientStatementAsync(new BillingStatementLaunchRequest
        {
            ExternalClientId = "client-1",
            DateRangePreset = BillingStatementDateRangePresets.ThisMonth,
            Status = BillingStatementStatuses.All,
            IncludeCredits = true,
            IncludePayments = false,
            IncludeAging = true,
        });

        Assert.Equal("/client_statement", httpClient.LastPostStreamEndpoint);
        var payload = Assert.IsType<Dictionary<string, object?>>(httpClient.LastPostStreamBody);
        Assert.Equal("client-1", payload["client_id"]);
        Assert.Equal(true, payload["show_credits_table"]);
        Assert.False(payload.ContainsKey("show_payments_table"));
        Assert.Equal(true, payload["show_aging_table"]);
        Assert.Equal(new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).ToString("yyyy-MM-dd"), payload["start_date"]);
        Assert.Equal(new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month)).ToString("yyyy-MM-dd"), payload["end_date"]);
        Assert.Equal("application/pdf", document.ContentType);
        Assert.Equal("statement.pdf", document.FileName);
    }

    [Fact]
    public async Task GetClientStatementAsync_OmitsUncheckedStatementTableFlags()
    {
        await using var readContext = CreateReadContext();
        var client = new InvoiceNinjaClientResponse { Id = "client-1", Name = "Acme" };
        var httpClient = new RecordingInvoiceNinjaHttpClient(client);
        var service = CreateBillingService(readContext, httpClient);

        await service.GetClientStatementAsync(new BillingStatementLaunchRequest
        {
            ExternalClientId = "client-1",
            DateRangePreset = BillingStatementDateRangePresets.AllOutstanding,
            Status = BillingStatementStatuses.All,
            IncludeCredits = false,
            IncludePayments = false,
            IncludeAging = false,
        });

        var payload = Assert.IsType<Dictionary<string, object?>>(httpClient.LastPostStreamBody);
        Assert.Equal("client-1", payload["client_id"]);
        Assert.Equal("2000-01-01", payload["start_date"]);
        Assert.Equal(DateTime.UtcNow.Date.ToString("yyyy-MM-dd"), payload["end_date"]);
        Assert.False(payload.ContainsKey("show_credits_table"));
        Assert.False(payload.ContainsKey("show_payments_table"));
        Assert.False(payload.ContainsKey("show_aging_table"));
    }

    [Fact]
    public async Task GetClientStatementAsync_UsesConfiguredBusinessTimezoneForAllOutstandingEndDate()
    {
        await using var readContext = CreateReadContext();
        var client = new InvoiceNinjaClientResponse { Id = "client-1", Name = "Acme" };
        var httpClient = new RecordingInvoiceNinjaHttpClient(client);
        var settingsService = new InMemorySettingsService();
        settingsService.Update(new UpdateSettingsRequest
        {
            CompanyName = "JB2026 Printing",
            TimeZone = "Asia/Kuala_Lumpur",
            CurrencyCode = "MYR",
            EnableLegacyFallback = true,
            OwnerName = "Marche Label & Printing Limited",
            NextOrderNumber = "168360",
            NextProductNumber = "005356",
            NextQuotationNumber = "170024",
            CommonQueryIndex = 2,
            CompletedQueryIndex = 1,
            ScheduleQueryRange = 1,
            GmailAccount = "job.book@marchehk.com",
            GmailPassword = "24110810",
            DateFormatPreference = SettingsResponse.DefaultDateFormatPreference,
        });

        var service = CreateBillingService(
            readContext,
            httpClient,
            settingsService,
            new FixedTimeProvider(new DateTimeOffset(2026, 6, 2, 17, 30, 0, TimeSpan.Zero)));

        await service.GetClientStatementAsync(new BillingStatementLaunchRequest
        {
            ExternalClientId = "client-1",
            DateRangePreset = BillingStatementDateRangePresets.AllOutstanding,
            Status = BillingStatementStatuses.All,
        });

        var payload = Assert.IsType<Dictionary<string, object?>>(httpClient.LastPostStreamBody);
        Assert.Equal("2026-06-03", payload["end_date"]);
    }

    [Fact]
    public async Task CreateClientStatementLaunch_ReturnsLaunchUrlForNormalizedRequest()
    {
        await using var readContext = CreateReadContext();
        var client = new InvoiceNinjaClientResponse { Id = "client-1", Name = "Acme" };
        var service = CreateBillingService(readContext, new RecordingInvoiceNinjaHttpClient(client));
        var controller = CreateController(service);

        var result = await controller.CreateClientStatementLaunch(new BillingStatementLaunchRequest
        {
            ExternalClientId = "client-1",
            DateRangePreset = BillingStatementDateRangePresets.ThisYear,
            Status = BillingStatementStatuses.All,
            IncludeCredits = true,
            IncludePayments = true,
            IncludeAging = false,
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<BillingStatementLaunchResponse>(ok.Value);
        Assert.Contains("/api/v2/billing/statements/client?", response.LaunchUrl, StringComparison.Ordinal);
        Assert.Contains("externalClientId=client-1", response.LaunchUrl, StringComparison.Ordinal);
        Assert.Contains("dateRangePreset=This%20Year", response.LaunchUrl, StringComparison.Ordinal);
        Assert.Contains("status=All", response.LaunchUrl, StringComparison.Ordinal);
        Assert.Contains("includeCredits=true", response.LaunchUrl, StringComparison.Ordinal);
        Assert.Contains("includePayments=true", response.LaunchUrl, StringComparison.Ordinal);
        Assert.Contains("includeAging=false", response.LaunchUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetClientStatement_ReturnsServiceUnavailableWhenUpstreamFails()
    {
        await using var readContext = CreateReadContext();
        var client = new InvoiceNinjaClientResponse { Id = "client-1", Name = "Acme" };
        var service = CreateBillingService(readContext, new RecordingInvoiceNinjaHttpClient(client)
        {
            PostStreamException = BillingException.ServiceUnavailable(503)
        });
        var controller = CreateController(service);

        var result = await controller.GetClientStatement(new BillingStatementLaunchRequest
        {
            ExternalClientId = "client-1",
            DateRangePreset = BillingStatementDateRangePresets.AllOutstanding,
            Status = BillingStatementStatuses.All,
        });

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
        var error = Assert.IsType<BillingErrorResponse>(objectResult.Value);
        Assert.Equal("SERVICE_UNAVAILABLE", error.ErrorCode);
    }

    private static BillingService CreateBillingService(
        JB5LegacyReadContext readContext,
        IInvoiceNinjaHttpClient invoiceNinjaHttpClient,
        ISettingsService? settingsService = null,
        TimeProvider? timeProvider = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(readContext);
        if (settingsService != null)
        {
            services.AddSingleton(settingsService);
        }

        if (timeProvider != null)
        {
            services.AddSingleton(timeProvider);
        }

        var provider = services.BuildServiceProvider();

        return new BillingService(
            invoiceNinjaHttpClient,
            Microsoft.Extensions.Options.Options.Create(new BillingOptions()),
            provider,
            NullLogger<BillingService>.Instance);
    }

    private static BillingController CreateController(IBillingService billingService)
    {
        var controller = new BillingController(
            billingService,
            new StubCurrentUserProfileService(),
            new StubPaperlessNgxService(),
            NullLogger<BillingController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private static JB5LegacyReadContext CreateReadContext()
    {
        var options = new DbContextOptionsBuilder<JB5LegacyReadContext>()
            .UseInMemoryDatabase($"billing-client-statement-{Guid.NewGuid():N}")
            .Options;
        return new JB5LegacyReadContext(options);
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

    private sealed class StubPaperlessNgxService : IPaperlessNgxService
    {
        public Task<IReadOnlyList<PaperlessNgxDocumentResponse>> SearchDocumentsAsync(string query, string? searchText = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PaperlessNgxDocumentResponse>>([]);

        public Task<PaperlessNgxUploadResult> UploadJobOrderAsync(string title, string fileName, byte[] pdfContent, string? customerName, string? tagName, CancellationToken cancellationToken = default)
            => Task.FromResult(new PaperlessNgxUploadResult { AlreadyExists = false, DocumentId = 1 });

        public Task<PaperlessNgxUploadResult> UploadInvoiceAsync(string title, string fileName, byte[] pdfContent, string? clientName, string? tagName, CancellationToken cancellationToken = default)
            => Task.FromResult(new PaperlessNgxUploadResult { AlreadyExists = false, DocumentId = 1 });
    }

    private sealed class RecordingInvoiceNinjaHttpClient(InvoiceNinjaClientResponse? client = null) : IInvoiceNinjaHttpClient
    {
        public string? LastPostStreamEndpoint { get; private set; }

        public object? LastPostStreamBody { get; private set; }

        public InvoiceNinjaBinaryResponse PostStreamResponse { get; set; } = new()
        {
            Content = [1],
            ContentType = "application/pdf",
            FileName = "client-statement.pdf"
        };

        public BillingException? PostStreamException { get; set; }

        public Task<T?> GetAsync<T>(string endpoint) where T : class
        {
            if (typeof(T) == typeof(InvoiceNinjaClientResponse) && endpoint == $"/clients/{client?.Id}")
            {
                return Task.FromResult(client as T);
            }

            return Task.FromResult<T?>(null);
        }

        public Task<T> PostAsync<T>(string endpoint, object body) where T : class => throw new NotSupportedException();

        public Task<InvoiceNinjaBinaryResponse> PostStreamAsync(string endpoint, object body)
        {
            LastPostStreamEndpoint = endpoint;
            LastPostStreamBody = body;

            if (PostStreamException != null)
            {
                throw PostStreamException;
            }

            return Task.FromResult(PostStreamResponse);
        }

        public Task<T> PutAsync<T>(string endpoint, object body) where T : class => throw new NotSupportedException();

        public Task<bool> IsConnectedAsync() => Task.FromResult(true);

        public (bool isValid, string errorMessage) ValidateConfiguration() => (true, string.Empty);

        public Task<byte[]?> GetStreamAsync(string endpoint) => Task.FromResult<byte[]?>(null);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
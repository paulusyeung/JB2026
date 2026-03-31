using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using JB2026.WebApp;
using JB2026.WebApp.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JB2026.WebApp.Tests;

public sealed class UiSliceRoutingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UiSliceRoutingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EnabledFlag_RoutesToSpaIndex()
    {
        var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["UiModernization:Slices:jobs:DisplayName"] = "Jobs",
            ["UiModernization:Slices:jobs:Enabled"] = "true",
            ["UiModernization:Slices:jobs:Prefixes:0"] = "/jobs",
            ["UiModernization:LegacyBaseUrl"] = ""
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/jobs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("JB2026 UI Modernization", html, StringComparison.Ordinal);
        Assert.Contains("<div id=\"app\"></div>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisabledFlag_RedirectsToLegacyRoute_WhenLegacyBaseUrlConfigured()
    {
        var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["UiModernization:Slices:jobs:DisplayName"] = "Jobs",
            ["UiModernization:Slices:jobs:Enabled"] = "false",
            ["UiModernization:Slices:jobs:Prefixes:0"] = "/jobs",
            ["UiModernization:LegacyBaseUrl"] = "https://legacy.example/"
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/jobs?status=open");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal("https://legacy.example/jobs?status=open", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task DisabledLegacyAspxRoute_RedirectsToLegacyRoute_WhenPrefixMatches()
    {
        var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["UiModernization:Slices:job-order:DisplayName"] = "Job Order",
            ["UiModernization:Slices:job-order:Enabled"] = "false",
            ["UiModernization:Slices:job-order:Prefixes:0"] = "/joborder",
            ["UiModernization:LegacyBaseUrl"] = "https://legacy.example/"
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/JobOrder/JobStatsPage.aspx?status=open");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal("https://legacy.example/JobOrder/JobStatsPage.aspx?status=open", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task DisabledDashedLegacyRoute_RedirectsToLegacyRoute_WhenPrefixMatches()
    {
        var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["UiModernization:Slices:job-order:DisplayName"] = "Job Order",
            ["UiModernization:Slices:job-order:Enabled"] = "false",
            ["UiModernization:Slices:job-order:Prefixes:0"] = "/joborder",
            ["UiModernization:Slices:job-order:Prefixes:1"] = "/job-order",
            ["UiModernization:LegacyBaseUrl"] = "https://legacy.example/"
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/job-order/OrderList_MasterDetailPage.aspx?status=open");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal("https://legacy.example/job-order/OrderList_MasterDetailPage.aspx?status=open", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task LegacySlicesEndpoint_ReturnsCatalogWithFlagState()
    {
        var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["UiModernization:Slices:job-order:DisplayName"] = "Job Order",
            ["UiModernization:Slices:job-order:Enabled"] = "true",
            ["UiModernization:Slices:job-order:Prefixes:0"] = "/job-order"
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/ui/legacy-slices");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"key\":\"job-order\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"legacyFolder\":\"JobOrder\"", body, StringComparison.Ordinal);
        Assert.Contains("\"modernPath\":\"/job-order\"", body, StringComparison.Ordinal);
        Assert.Contains("\"enabled\":true", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LegacySliceStatusEndpoint_ReturnsSpaMode_WhenSliceEnabled()
    {
        var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["UiModernization:Slices:job-order:DisplayName"] = "Job Order",
            ["UiModernization:Slices:job-order:Enabled"] = "true",
            ["UiModernization:Slices:job-order:Prefixes:0"] = "/joborder",
            ["UiModernization:Slices:job-order:Prefixes:1"] = "/job-order"
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/ui/legacy-slices/job-order/status");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"handlingMode\":\"spa\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"resolvedTargetUrl\":\"/app/index.html\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LegacySliceStatusEndpoint_ReturnsLegacyRedirect_WhenSliceDisabledAndLegacyBaseConfigured()
    {
        var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["UiModernization:Slices:job-order:DisplayName"] = "Job Order",
            ["UiModernization:Slices:job-order:Enabled"] = "false",
            ["UiModernization:Slices:job-order:Prefixes:0"] = "/joborder",
            ["UiModernization:Slices:job-order:Prefixes:1"] = "/job-order",
            ["UiModernization:LegacyBaseUrl"] = "https://legacy.example/"
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/ui/legacy-slices/job-order/status");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"handlingMode\":\"legacy-redirect\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://legacy.example/JobOrder/JobStatsPage.aspx", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LegacySliceReadinessEndpoint_ReportsRedirectReadiness_WhenLegacyBaseConfigured()
    {
        var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["UiModernization:Slices:job-order:DisplayName"] = "Job Order",
            ["UiModernization:Slices:job-order:Enabled"] = "false",
            ["UiModernization:Slices:job-order:Prefixes:0"] = "/joborder",
            ["UiModernization:Slices:job-order:Prefixes:1"] = "/job-order",
            ["UiModernization:LegacyBaseUrl"] = "https://legacy.example/"
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/ui/legacy-slices/job-order/readiness");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"legacyBaseConfigured\":true", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"legacyRedirectRoutes\":2", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"apiDependencies\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"implemented\":true", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"blockers\":[]", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LegacySliceReadinessEndpoint_ReportsPlaceholderBlocker_WhenDisabledWithoutLegacyBase()
    {
        var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["UiModernization:Slices:job-order:DisplayName"] = "Job Order",
            ["UiModernization:Slices:job-order:Enabled"] = "false",
            ["UiModernization:Slices:job-order:Prefixes:0"] = "/joborder",
            ["UiModernization:Slices:job-order:Prefixes:1"] = "/job-order",
            ["UiModernization:LegacyBaseUrl"] = ""
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/ui/legacy-slices/job-order/readiness");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"legacyBaseConfigured\":false", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"legacyPlaceholderRoutes\":2", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Legacy base URL is not configured", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LegacySliceReadinessEndpoint_ReportsImplementedApiContracts_ForStockSlice()
    {
        var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["UiModernization:Slices:stock:DisplayName"] = "Stock",
            ["UiModernization:Slices:stock:Enabled"] = "false",
            ["UiModernization:Slices:stock:Prefixes:0"] = "/stock",
            ["UiModernization:LegacyBaseUrl"] = "https://legacy.example/"
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/ui/legacy-slices/stock/readiness");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"apiDependencies\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/api/v2/stock/products", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"implemented\":true", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("API contracts pending implementation", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LegacySliceActionPlanEndpoint_IncludesLegacyBaseConfigurationStep_WhenDisabledWithoutLegacyBase()
    {
        var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["UiModernization:Slices:job-order:DisplayName"] = "Job Order",
            ["UiModernization:Slices:job-order:Enabled"] = "false",
            ["UiModernization:Slices:job-order:Prefixes:0"] = "/joborder",
            ["UiModernization:Slices:job-order:Prefixes:1"] = "/job-order",
            ["UiModernization:LegacyBaseUrl"] = ""
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/ui/legacy-slices/job-order/action-plan");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Configure legacy redirect target", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UiModernization:LegacyBaseUrl", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LegacySliceActionPlanEndpoint_DoesNotIncludePendingApiContractSteps_ForStockSlice()
    {
        var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["UiModernization:Slices:stock:DisplayName"] = "Stock",
            ["UiModernization:Slices:stock:Enabled"] = "false",
            ["UiModernization:Slices:stock:Prefixes:0"] = "/stock",
            ["UiModernization:LegacyBaseUrl"] = "https://legacy.example/"
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/ui/legacy-slices/stock/action-plan");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Implement API contract", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApiRequests_AreForwardedToConfiguredApiBaseUrl()
    {
        HttpRequestMessage? forwardedRequest = null;
        string? forwardedBody = null;

        var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Vite:ApiBaseUrl"] = "https://api.example"
        }).WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddHttpClient(ApiProxyMiddleware.ClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(async request =>
                    {
                        forwardedRequest = request;
                        forwardedBody = request.Content is null
                            ? null
                            : await request.Content.ReadAsStringAsync();

                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                """{"accessToken":"proxy-token"}""",
                                Encoding.UTF8,
                                "application/json")
                        };
                    }));
            });
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.PostAsJsonAsync("/api/v2/auth/token", new
        {
            username = "admin",
            password = "password123"
        });

        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("proxy-token", responseBody, StringComparison.Ordinal);
        Assert.NotNull(forwardedRequest);
        Assert.Equal(HttpMethod.Post, forwardedRequest!.Method);
        Assert.Equal("https://api.example/api/v2/auth/token", forwardedRequest.RequestUri!.ToString());
        Assert.Equal("application/json", forwardedRequest.Content?.Headers.ContentType?.MediaType);
        Assert.NotNull(forwardedBody);
        Assert.Contains("\"username\":\"admin\"", forwardedBody, StringComparison.Ordinal);
        Assert.Contains("\"password\":\"password123\"", forwardedBody, StringComparison.Ordinal);
    }

    private WebApplicationFactory<Program> CreateFactory(IReadOnlyDictionary<string, string?> overrides)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(overrides);
            });
        });
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }
}

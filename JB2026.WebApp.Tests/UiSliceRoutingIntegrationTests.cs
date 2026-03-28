using System.Net;
using JB2026.WebApp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

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
}

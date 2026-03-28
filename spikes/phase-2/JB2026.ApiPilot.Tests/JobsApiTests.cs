using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using JB2026.ApiPilot.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JB2026.ApiPilot.Tests;

public sealed class JobsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public JobsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RangeEndpoint_requires_bearer_token()
    {
        var response = await _client.GetAsync("/api/v1/jobs/range?startOn=2026-03-27&days=10");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RangeEndpoint_matches_legacy_baseline_snapshot()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await CreateTokenAsync());

        var response = await _client.GetAsync("/api/v1/jobs/range?startOn=2026-03-27&days=10");
        response.EnsureSuccessStatusCode();

        var actual = await response.Content.ReadFromJsonAsync<List<JobListItem>>(SerializerOptions);
        var expected = await ReadBaselineAsync<List<JobListItem>>("jobs-range.json");

        Assert.NotNull(actual);
        Assert.Equivalent(expected, actual, strict: true);
    }

    [Fact]
    public async Task DetailEndpoint_matches_legacy_baseline_snapshot()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await CreateTokenAsync());

        var response = await _client.GetAsync("/api/v1/jobs/1e84b2e5-3f73-4d60-9d0d-08dc50c00001");
        response.EnsureSuccessStatusCode();

        var actual = await response.Content.ReadFromJsonAsync<JobDetail>(SerializerOptions);
        var expected = await ReadBaselineAsync<JobDetail>("job-detail.json");

        Assert.NotNull(actual);
        Assert.Equivalent(expected, actual, strict: true);
    }

    private async Task<string> CreateTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/token", new TokenRequest
        {
            DisplayName = "Phase 2 API Test",
            Role = "Manager"
        });

        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(SerializerOptions);
        return tokenResponse!.AccessToken;
    }

    private static async Task<T> ReadBaselineAsync<T>(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Baselines", fileName);
        await using var stream = File.OpenRead(path);
        var payload = await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions);
        return payload!;
    }
}
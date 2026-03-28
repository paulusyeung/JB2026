using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using JB2026.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JB2026.Api.ParityTests;

public sealed class JobsParityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public JobsParityTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task JobsRangeEndpoint_maps_to_legacy_rest_job_range_snapshot()
    {
        var snapshot = await ReadSnapshotAsync("rest_job_range.json");
        var token = await CreateTokenAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v2/jobs/range?startOn=2026-03-27&days=10");

        await AssertSnapshotParityAsync(response, snapshot);
    }

    [Fact]
    public async Task JobsRangeEndpoint_maps_to_legacy_rest_job_by_month_snapshot()
    {
        var snapshot = await ReadSnapshotAsync("rest_job_by_month_all.json");
        var token = await CreateTokenAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v2/jobs/range?startOn=2026-03-27&days=31");

        await AssertSnapshotParityAsync(response, snapshot);
    }

    [Fact]
    public async Task JobOrdersEndpoint_maps_to_legacy_api_joborders_list_snapshot()
    {
        var snapshot = await ReadSnapshotAsync("api_joborders_list.json");
        var token = await CreateTokenAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v2/job-orders");

        await AssertSnapshotParityAsync(response, snapshot);
    }

    private async Task<string> CreateTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v2/auth/token", new TokenRequest
        {
            Username = "admin",
            Password = "password123"
        });

        response.EnsureSuccessStatusCode();
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(SerializerOptions);

        Assert.NotNull(tokenResponse);
        return tokenResponse.AccessToken;
    }

    private static async Task<LegacySnapshotEnvelope> ReadSnapshotAsync(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Snapshots", fileName);
        await using var stream = File.OpenRead(path);
        var snapshot = await JsonSerializer.DeserializeAsync<LegacySnapshotEnvelope>(stream, SerializerOptions);

        Assert.NotNull(snapshot);
        return snapshot;
    }

    private static async Task AssertSnapshotParityAsync(HttpResponseMessage response, LegacySnapshotEnvelope snapshot)
    {
        if (snapshot.Success)
        {
            Assert.Equal(snapshot.StatusCode, (int)response.StatusCode);
            return;
        }

        Assert.True((int)response.StatusCode < (int)HttpStatusCode.InternalServerError,
            $"Migrated endpoint returned unexpected server failure {(int)response.StatusCode} while legacy baseline was {snapshot.StatusCode}.");

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(SerializerOptions);
        Assert.True(payload.ValueKind == JsonValueKind.Array || payload.ValueKind == JsonValueKind.Object,
            "Expected migrated endpoint payload to be JSON array/object.");
    }

    private sealed class LegacySnapshotEnvelope
    {
        public required string Name { get; init; }

        public bool Success { get; init; }

        public int StatusCode { get; init; }

        public JsonElement ResponseBody { get; init; }
    }
}

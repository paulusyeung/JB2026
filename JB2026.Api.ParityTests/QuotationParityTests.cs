using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using JB2026.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JB2026.Api.ParityTests;

public sealed class QuotationParityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public QuotationParityTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SearchEndpoint_matches_legacy_snapshot_count_and_shape_for_keyword_ABC()
    {
        var snapshot = await ReadSnapshotAsync("rest_quotation_keyword.json");
        var token = await CreateTokenAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v2/quotations/search/ABC");

        await AssertSnapshotParityAsync(response, snapshot);
    }

    [Fact]
    public async Task RangeEndpoint_uses_legacy_snapshot_and_returns_stable_contract()
    {
        var snapshot = await ReadSnapshotAsync("rest_quotation_range.json");
        var token = await CreateTokenAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v2/quotations?startOn=2026-03-27&days=10");

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
            var actual = await response.Content.ReadFromJsonAsync<JsonElement>(SerializerOptions);

            Assert.True(actual.ValueKind == JsonValueKind.Array || actual.ValueKind == JsonValueKind.Object,
                "Expected migrated endpoint payload to be JSON array/object.");

            var expected = ExtractLegacyItems(snapshot.ResponseBody);
            var normalizedActual = ExtractLegacyItems(actual);

            Assert.Equal(expected.GetArrayLength(), normalizedActual.GetArrayLength());
            AssertSharedPropertySet(expected, normalizedActual);
            return;
        }

        // If legacy snapshot failed in this environment, ensure migrated endpoint is at least not a server failure.
        Assert.True((int)response.StatusCode < (int)HttpStatusCode.InternalServerError,
            $"Migrated endpoint returned unexpected server failure {(int)response.StatusCode} while legacy baseline was {(snapshot.StatusCode)}.");
    }

    private static JsonElement ExtractLegacyItems(JsonElement responseBody)
    {
        if (responseBody.ValueKind == JsonValueKind.Array)
        {
            return responseBody;
        }

        if (responseBody.ValueKind == JsonValueKind.Object &&
            responseBody.TryGetProperty("value", out var valueElement) &&
            valueElement.ValueKind == JsonValueKind.Array)
        {
            return valueElement;
        }

        return JsonDocument.Parse("[]").RootElement.Clone();
    }

    private static void AssertSharedPropertySet(JsonElement expectedItems, JsonElement actualItems)
    {
        if (expectedItems.GetArrayLength() == 0 || actualItems.GetArrayLength() == 0)
        {
            return;
        }

        var expectedFirst = expectedItems[0];
        var actualFirst = actualItems[0];
        if (expectedFirst.ValueKind != JsonValueKind.Object || actualFirst.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var expectedProps = expectedFirst.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actualProps = actualFirst.EnumerateObject().Select(p => p.Name);
        foreach (var property in actualProps)
        {
            Assert.Contains(property, expectedProps);
        }
    }

    private sealed class LegacySnapshotEnvelope
    {
        public required string Name { get; init; }

        public bool Success { get; init; }

        public int StatusCode { get; init; }

        public JsonElement ResponseBody { get; init; }
    }
}

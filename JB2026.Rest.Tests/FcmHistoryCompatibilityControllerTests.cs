using System.Net;
using System.Text.Json;
using JB2026.EfCore.Models;

namespace JB2026.Rest.Tests;

/// <summary>
/// Tests for FcmHistoryCompatibilityController — verifies auth guard, pagination,
/// and single-item lookup against an InMemory EF Core database.
/// </summary>
public sealed class FcmHistoryCompatibilityControllerTests : IClassFixture<RestTestFixture>
{
    private readonly RestTestFixture _factory;

    public FcmHistoryCompatibilityControllerTests(RestTestFixture factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_NoAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/FCMHistory");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithAuth_EmptyDb_ReturnsOkEmptyArray()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/FCMHistory");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
    }

    [Fact]
    public async Task GetByPage_WithAuth_EmptyDb_ReturnsOkEmptyArray()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/FCMHistory/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
    }

    [Fact]
    public async Task GetById_NonExistentGuid_ReturnsNotFound()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/FCMHistory/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingRecord_ReturnsOk()
    {
        var recordId = Guid.NewGuid();
        var userSid = RestTestFixture.TestAdminUserId;

        await _factory.SeedAsync(ctx =>
        {
            ctx.FCMHistories.Add(new FCMHistory
            {
                FCMHistoryId = recordId,
                Topic = "everyone",
                DeliveredOn = DateTime.Now,
                UserIdList = userSid.ToString(),
                MessageTitle = "Test notification",
                MessageBody = "Test body",
            });
        });

        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/FCMHistory/{recordId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync());
        Assert.Equal(recordId.ToString(), json.GetProperty("fcmHistoryId").GetString());
    }
}

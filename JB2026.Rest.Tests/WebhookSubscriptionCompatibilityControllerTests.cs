using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace JB2026.Rest.Tests;

/// <summary>
/// Tests for WebhookSubscriptionCompatibilityController — verifies full CRUD surface
/// (POST create, GET list, GET by id, PUT update, DELETE soft-delete) and auth guard.
/// </summary>
public sealed class WebhookSubscriptionCompatibilityControllerTests : IClassFixture<RestTestFixture>
{
    private readonly RestTestFixture _factory;

    public WebhookSubscriptionCompatibilityControllerTests(RestTestFixture factory)
    {
        _factory = factory;
    }

    // -----------------------------------------------------------------------
    // Auth guard
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Get_NoAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/WebhookSubscription");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // POST — create
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Post_ValidPayload_Returns201WithLocation()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var payload = new { Url = "https://example.com/hook", EventTypes = "order.created" };
        var response = await client.PostAsJsonAsync("/api/WebhookSubscription", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Post_MissingUrl_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var payload = new { Url = (string?)null, EventTypes = "order.created" };
        var response = await client.PostAsJsonAsync("/api/WebhookSubscription", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_MissingEventTypes_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var payload = new { Url = "https://example.com/hook", EventTypes = (string?)null };
        var response = await client.PostAsJsonAsync("/api/WebhookSubscription", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // GET — list and by id
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Get_AfterPost_ContainsNewSubscription()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var url = $"https://example.com/hook-{Guid.NewGuid():N}";

        await client.PostAsJsonAsync("/api/WebhookSubscription",
            new { Url = url, EventTypes = "order.shipped" });

        var response = await client.GetAsync("/api/WebhookSubscription");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(url, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/WebhookSubscription/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_AfterPost_ReturnsSubscription()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var url = $"https://example.com/hook-{Guid.NewGuid():N}";

        var postResponse = await client.PostAsJsonAsync("/api/WebhookSubscription",
            new { Url = url, EventTypes = "order.ready" });
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        // Extract id from Location header: /api/WebhookSubscription/{id}
        var location = postResponse.Headers.Location!.ToString();
        var id = location.Split('/').Last();

        var getResponse = await client.GetAsync($"/api/WebhookSubscription/{id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var json = JsonSerializer.Deserialize<JsonElement>(
            await getResponse.Content.ReadAsStringAsync());
        Assert.Equal(url, json.GetProperty("url").GetString());
    }

    // -----------------------------------------------------------------------
    // PUT — update
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Put_NonExistentId_Returns404()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var payload = new { Url = "https://example.com/updated", EventTypes = "order.updated" };
        var response = await client.PutAsJsonAsync("/api/WebhookSubscription/999999", payload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_ExistingId_Returns200WithUpdatedUrl()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var originalUrl = $"https://example.com/original-{Guid.NewGuid():N}";
        var updatedUrl = $"https://example.com/updated-{Guid.NewGuid():N}";

        var postResponse = await client.PostAsJsonAsync("/api/WebhookSubscription",
            new { Url = originalUrl, EventTypes = "order.created" });
        var id = postResponse.Headers.Location!.ToString().Split('/').Last();

        var putResponse = await client.PutAsJsonAsync(
            $"/api/WebhookSubscription/{id}",
            new { Url = updatedUrl, EventTypes = "order.created" });

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var json = JsonSerializer.Deserialize<JsonElement>(
            await putResponse.Content.ReadAsStringAsync());
        Assert.Equal(updatedUrl, json.GetProperty("url").GetString());
    }

    // -----------------------------------------------------------------------
    // DELETE — soft-delete
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Delete_NonExistentId_Returns404()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/api/WebhookSubscription/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingId_Returns200_ThenGetByIdReturns404()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var url = $"https://example.com/delete-{Guid.NewGuid():N}";

        var postResponse = await client.PostAsJsonAsync("/api/WebhookSubscription",
            new { Url = url, EventTypes = "order.created" });
        var id = postResponse.Headers.Location!.ToString().Split('/').Last();

        var deleteResponse = await client.DeleteAsync($"/api/WebhookSubscription/{id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        // Soft-deleted subscription should no longer be found
        var getResponse = await client.GetAsync($"/api/WebhookSubscription/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}

using System.Net;
using System.Text.Json;

namespace JB2026.Rest.Tests;

/// <summary>
/// Tests for FileAgentCompatibilityController — verifies the filingCategory list route
/// (static list, no DB dependency) and the auth guard on the controller.
/// </summary>
public sealed class FileAgentCompatibilityControllerTests : IClassFixture<RestTestFixture>
{
    private readonly RestTestFixture _factory;

    public FileAgentCompatibilityControllerTests(RestTestFixture factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetFilingCategory_NoAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/fileAgent/filingCategory");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFilingCategory_WithAuth_ReturnsOk()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/fileAgent/filingCategory");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetFilingCategory_WithAuth_ReturnsNonEmptyStringArray()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/fileAgent/filingCategory");
        var body = await response.Content.ReadAsStringAsync();
        var categories = JsonSerializer.Deserialize<string[]>(body);

        Assert.NotNull(categories);
        Assert.NotEmpty(categories);
    }

    [Theory]
    [InlineData("CUPS")]
    [InlineData("CIP3")]
    [InlineData("VPS")]
    [InlineData("Blueprint")]
    [InlineData("Plate")]
    [InlineData("Film")]
    public async Task GetFilingCategory_WithAuth_ContainsExpectedCategories(string expectedCategory)
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/fileAgent/filingCategory");
        var body = await response.Content.ReadAsStringAsync();
        var categories = JsonSerializer.Deserialize<string[]>(body);

        Assert.NotNull(categories);
        Assert.Contains(expectedCategory, categories, StringComparer.OrdinalIgnoreCase);
    }
}

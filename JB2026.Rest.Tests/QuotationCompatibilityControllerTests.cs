using System.Net;
using System.Text.Json;

namespace JB2026.Rest.Tests;

public sealed class QuotationCompatibilityControllerTests : IClassFixture<RestTestFixture>
{
    private readonly RestTestFixture _factory;
    private static readonly Guid KnownQuotationId = Guid.Parse("2a84b2e5-3f73-4d60-9d0d-08dc50c00001");

    public QuotationCompatibilityControllerTests(RestTestFixture factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetQt_NoAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/Qt/{DateTime.UtcNow:O}/7");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetQt_WithAuth_ReturnsLegacyProjectionFields()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/Qt/{DateTime.UtcNow:O}/30");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.Equal(JsonValueKind.Array, json.ValueKind);
        Assert.NotEqual(0, json.GetArrayLength());

        var first = json[0];
        Assert.True(first.TryGetProperty("quoteNumberIndexPair", out _));
        Assert.True(first.TryGetProperty("paperSheetSize", out _));
        Assert.True(first.TryGetProperty("totalCostB", out _));
        Assert.True(first.TryGetProperty("unitCostD", out _));
        Assert.True(first.TryGetProperty("retired", out _));
    }

    [Fact]
    public async Task GetQtByKeyword_ShortKeyword_ReturnsEmptyArray()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/Qt/Keyword/ab");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", (await response.Content.ReadAsStringAsync()).Trim());
    }

    [Fact]
    public async Task GetQtPdf_UnknownId_ReturnsNotFound()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/Qt/pdf/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetQtPdf_KnownId_ReturnsPdfWithReportContent()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/Qt/pdf/{KnownQuotationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(content);

        var ascii = System.Text.Encoding.ASCII.GetString(content);
        Assert.Contains("Quotation 61024-1", ascii, StringComparison.Ordinal);
        Assert.Contains("Northwind Print Co.", ascii, StringComparison.Ordinal);
        Assert.Contains("Retail Packaging Artwork", ascii, StringComparison.Ordinal);
    }
}

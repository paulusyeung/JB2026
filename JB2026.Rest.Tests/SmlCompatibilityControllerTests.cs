using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using JB2026.EfCore.Models;
using Microsoft.Extensions.Configuration;

namespace JB2026.Rest.Tests;

public sealed class SmlCompatibilityControllerTests : IClassFixture<RestTestFixture>
{
    private readonly RestTestFixture _factory;

    public SmlCompatibilityControllerTests(RestTestFixture factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/SML/Keyword/abc")]
    [InlineData("/api/SML/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/SML/file/00000000-0000-0000-0000-000000000001")]
    public async Task SmlRoutes_NoAuth_ReturnUnauthorized(string route)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetByRange_InvalidDaysZero_ReturnsBadRequest()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/SML/{DateTime.UtcNow:O}/0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetByRange_InvalidDaysTooLarge_ReturnsBadRequest()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/SML/{DateTime.UtcNow:O}/367");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetByKeyword_ShortKeyword_ReturnsOkEmptyArray()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/SML/Keyword/ab");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", (await response.Content.ReadAsStringAsync()).Trim());
    }

    [Fact]
    public async Task GetById_UnknownGuid_ReturnsOkArray()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/SML/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
    }

    [Fact]
    public async Task GetFile_WithoutRootConfig_Returns501()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/SML/file/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        var json = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync());
        Assert.Equal(501, json.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task GetFile_WithConfiguredRootAndSeededHeader_ReturnsRtfFile()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "jb2026-rest-sml-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var headerId = Guid.NewGuid();
        const string fileName = "sample.rtf";
        var fileBytes = new byte[] { 123, 92, 114, 116, 102, 49, 125 }; // {\rtf1}
        await File.WriteAllBytesAsync(Path.Combine(tempRoot, fileName), fileBytes);

        await _factory.SeedAsync(ctx =>
        {
            ctx.SmlRtfHeaders.Add(new SmlRtfHeader
            {
                HeaderId = headerId,
                RtfFileName = fileName,
                PurchaseOrder = "PO-TEST",
                OrderedOn = DateTime.Now,
                OrderedBy = "tester",
                CreatedOn = DateTime.Now,
                CreatedBy = Guid.NewGuid(),
                ModifiedOn = DateTime.Now,
                ModifiedBy = Guid.NewGuid(),
                Retired = false,
                RetiredOn = DateTime.Now
            });
        });

        try
        {
            using var scopedFactory = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["LegacyFiles:SmlFileRoot"] = tempRoot
                    })));

            using var client = scopedFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", RestTestFixture.GenerateToken(RestTestFixture.TestAdminUserId));

            var response = await client.GetAsync($"/api/SML/file/{headerId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/rtf", response.Content.Headers.ContentType?.MediaType);
            var returned = await response.Content.ReadAsByteArrayAsync();
            Assert.Equal(fileBytes, returned);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }
}

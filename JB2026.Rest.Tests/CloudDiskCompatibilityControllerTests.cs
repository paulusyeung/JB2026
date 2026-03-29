using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace JB2026.Rest.Tests;

public sealed class CloudDiskCompatibilityControllerTests : IClassFixture<RestTestFixture>
{
    private readonly RestTestFixture _factory;

    public CloudDiskCompatibilityControllerTests(RestTestFixture factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/CloudDisk/cups/10/1")]
    [InlineData("/api/CloudDisk/cups/keyword/10/demo")]
    [InlineData("/api/CloudDisk/thumbnail/10/file.jpg/100/100")]
    [InlineData("/api/CloudDisk/users/subadmin/WKS")]
    public async Task CloudDiskRoutes_NoAuth_ReturnUnauthorized(string route)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCups_WithAuthAndEmptyConfiguredRoot_ReturnsOk()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/CloudDisk/cups/10/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetThumbnailImage_WithAuthAndEmptyConfiguredRoot_ReturnsNotFound()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/CloudDisk/thumbnail/10/sample.jpg/120/90");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ActionEmail_WithAuth_ReturnsOkAcceptedPayload()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/CloudDisk/Action/Email/demo-id", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync());
        Assert.True(json.GetProperty("accepted").GetBoolean());
        Assert.True(json.GetProperty("recorded").GetBoolean());
    }

    [Fact]
    public async Task ActionEmail_WithTypedPayload_PersistsPayloadSummary()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var payload = new
        {
            recipient = "ops@example.com",
            remarks = "please review",
            expiryChecked = true,
            password = "abc",
            items = new[]
            {
                new
                {
                    idx = 1,
                    name = "sample.pdf",
                    path = "/cups/10/sample.pdf"
                }
            }
        };

        var response = await client.PostAsJsonAsync("/api/CloudDisk/Action/Email/demo-id", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var latest = await _factory.ReadAsync(ctx =>
            ctx.FCMHistories
                .OrderByDescending(x => x.DeliveredOn)
                .Select(x => x.MessageBody)
                .FirstOrDefaultAsync());

        Assert.NotNull(latest);
        Assert.Contains("ops@example.com", latest, StringComparison.Ordinal);
        Assert.Contains("sample.pdf", latest, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/api/CloudDisk/Action/Reprint/R-100", "Reprint")]
    [InlineData("/api/CloudDisk/Action/Output/Blueprint/BP-1", "OutputBlueprint")]
    [InlineData("/api/CloudDisk/Action/Output/Plate/PL-2", "OutputPlate")]
    [InlineData("/api/CloudDisk/Action/Output/Film/FM-3", "OutputFilm")]
    public async Task ActionRoutes_WithAuth_PersistLogEntry(string route, string expectedAction)
    {
        var before = await _factory.ReadAsync(ctx => ctx.FCMHistories.CountAsync());
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync(route, new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var after = await _factory.ReadAsync(ctx => ctx.FCMHistories.CountAsync());
        Assert.Equal(before + 1, after);

        var latest = await _factory.ReadAsync(ctx =>
            ctx.FCMHistories
                .OrderByDescending(x => x.DeliveredOn)
                .Select(x => x.MessageTitle)
                .FirstOrDefaultAsync());

        Assert.NotNull(latest);
        Assert.Contains(expectedAction, latest, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostFileUpload_InvalidGuid_ReturnsBadRequest()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var content = new MultipartFormDataContent();
        content.Add(new StringContent("test", Encoding.UTF8), "file", "sample.txt");

        var response = await client.PostAsync("/api/CloudDisk/fileAgent/upload/not-a-guid", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostFileUpload_ValidGuidWithoutFiles_ReturnsBadRequest()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsync($"/api/CloudDisk/fileAgent/upload/{Guid.NewGuid()}",
            new MultipartFormDataContent());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSubAdminUsers_WithAuth_ReturnsFilteredUsers()
    {
        var sid = Guid.NewGuid();

        await _factory.SeedAsync(ctx =>
        {
            ctx.Users.Add(new User
            {
                UserId = sid,
                UserSid = sid,
                UserType = 0,
                LoginName = "wks_admin",
                LoginPassword = "pass",
                Alias = "WKS-ALICE",
                Status = 1,
                CreatedOn = DateTime.Now,
                CreatedBy = sid,
                ModifiedOn = DateTime.Now,
                ModifiedBy = sid,
                Retired = false,
                RetiredOn = null,
                RetiredBy = null
            });

            ctx.Users.Add(new User
            {
                UserId = Guid.NewGuid(),
                UserSid = Guid.NewGuid(),
                UserType = 0,
                LoginName = "other",
                LoginPassword = "pass",
                Alias = "OTHER-BOB",
                Status = 1,
                CreatedOn = DateTime.Now,
                CreatedBy = sid,
                ModifiedOn = DateTime.Now,
                ModifiedBy = sid,
                Retired = false,
                RetiredOn = null,
                RetiredBy = null
            });
        });

        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/CloudDisk/users/subadmin/WKS");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("WKS-ALICE", body, StringComparison.Ordinal);
        Assert.DoesNotContain("OTHER-BOB", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetCups_WithConfiguredRoot_ReturnsSeededFiles()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "jb2026-rest-cloud-" + Guid.NewGuid().ToString("N"));
        var folder = Path.Combine(tempRoot, "cups", "10");
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "sample-a.txt"), "alpha");
        await File.WriteAllTextAsync(Path.Combine(folder, "sample-b.txt"), "beta");

        try
        {
            using var scopedFactory = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["LegacyFiles:CloudDiskRoot"] = tempRoot
                    })));

            using var client = scopedFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", RestTestFixture.GenerateToken(RestTestFixture.TestAdminUserId));

            var response = await client.GetAsync("/api/CloudDisk/cups/10/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("sample-a.txt", body, StringComparison.Ordinal);
            Assert.Contains("sample-b.txt", body, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    [Fact]
    public async Task GetThumbnailImage_WithConfiguredRoot_ReturnsFileContent()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "jb2026-rest-cloud-thumb-" + Guid.NewGuid().ToString("N"));
        var folder = Path.Combine(tempRoot, "thumbnail", "10");
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, "thumb.jpg");
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        await File.WriteAllBytesAsync(filePath, bytes);

        try
        {
            using var scopedFactory = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["LegacyFiles:CloudDiskRoot"] = tempRoot
                    })));

            using var client = scopedFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", RestTestFixture.GenerateToken(RestTestFixture.TestAdminUserId));

            var response = await client.GetAsync("/api/CloudDisk/thumbnail/10/thumb.jpg/100/100");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);
            var returned = await response.Content.ReadAsByteArrayAsync();
            Assert.Equal(bytes, returned);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    [Fact]
    public async Task PostFileUpload_WithConfiguredRoot_SavesFile()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "jb2026-rest-cloud-upload-" + Guid.NewGuid().ToString("N"));
        var orderId = Guid.NewGuid();

        try
        {
            using var scopedFactory = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["LegacyFiles:CloudDiskRoot"] = tempRoot
                    })));

            using var client = scopedFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", RestTestFixture.GenerateToken(RestTestFixture.TestAdminUserId));

            var multipart = new MultipartFormDataContent();
            multipart.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("hello")), "files", "upload.txt");

            var response = await client.PostAsync($"/api/CloudDisk/fileAgent/upload/{orderId}", multipart);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var expectedFile = Path.Combine(tempRoot, "uploads", orderId.ToString("N"), "upload.txt");
            Assert.True(File.Exists(expectedFile));
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

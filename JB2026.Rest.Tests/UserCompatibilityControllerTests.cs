using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Rest.Tests;

public sealed class UserCompatibilityControllerTests : IClassFixture<RestTestFixture>
{
    private readonly RestTestFixture _factory;

    public UserCompatibilityControllerTests(RestTestFixture factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCurrentUser_NoAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/User");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithAuth_ReturnsLegacyShape()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/User");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("loginName").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("loginPassword").GetString()));
        Assert.True(json.TryGetProperty("userRoleName", out _));
        Assert.True(json.TryGetProperty("userRole", out _));
        Assert.True(json.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task GetByUserId_WithAuth_ReturnsLegacyShape()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/User/{RestTestFixture.TestAdminUserId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

        Assert.Equal(RestTestFixture.TestAdminUserId.ToString(), json.GetProperty("userSid").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("loginName").GetString()));
    }

    [Fact]
    public async Task GetCurrentUser_WithSeededDbUser_PrefersDatabaseShape()
    {
        await _factory.SeedAsync(ctx =>
        {
            var existingUser = ctx.Users.SingleOrDefault(x => x.UserId == RestTestFixture.TestAdminUserId);
            if (existingUser is null)
            {
                ctx.Users.Add(new User
                {
                    UserId = RestTestFixture.TestAdminUserId,
                    UserType = 7,
                    UserSid = RestTestFixture.TestAdminUserId,
                    LoginName = "db-admin",
                    LoginPassword = "db-pass",
                    Alias = "DB Alias",
                    Status = 5,
                    CreatedOn = new DateTime(2025, 1, 1),
                    CreatedBy = RestTestFixture.TestAdminUserId,
                    ModifiedOn = new DateTime(2025, 2, 1),
                    ModifiedBy = RestTestFixture.TestAdminUserId,
                    Retired = false
                });
            }
            else
            {
                existingUser.UserType = 7;
                existingUser.LoginName = "db-admin";
                existingUser.LoginPassword = "db-pass";
                existingUser.Alias = "DB Alias";
                existingUser.Status = 5;
                existingUser.ModifiedOn = new DateTime(2025, 2, 1);
                existingUser.ModifiedBy = RestTestFixture.TestAdminUserId;
            }

            var existingInfo = ctx.UserInfos.SingleOrDefault(x => x.UserId == RestTestFixture.TestAdminUserId);
            if (existingInfo is null)
            {
                ctx.UserInfos.Add(new UserInfo
                {
                    UserId = RestTestFixture.TestAdminUserId,
                    PrimaryRec = true,
                    UserRole = 3,
                    CreatedOn = new DateTime(2025, 1, 1),
                    CreatedBy = RestTestFixture.TestAdminUserId,
                    ModifiedOn = new DateTime(2025, 2, 1),
                    ModifiedBy = RestTestFixture.TestAdminUserId,
                    Retired = false
                });
            }
            else
            {
                existingInfo.UserRole = 3;
                existingInfo.ModifiedOn = new DateTime(2025, 2, 1);
                existingInfo.ModifiedBy = RestTestFixture.TestAdminUserId;
            }
        });

        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/User");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        Assert.Equal("db-admin", json.GetProperty("loginName").GetString());
        Assert.Equal("db-pass", json.GetProperty("loginPassword").GetString());
        Assert.Equal("DB Alias", json.GetProperty("alias").GetString());
        Assert.Equal(7, json.GetProperty("userType").GetInt32());
        Assert.Equal(3, json.GetProperty("userRole").GetInt32());
        Assert.Equal("Manager", json.GetProperty("userRoleName").GetString());
    }

    [Fact]
    public async Task GetNotification_WithSeededMetadata_ReturnsParsedJsonPayload()
    {
        await _factory.SeedAsync(ctx =>
        {
            var existingUser = ctx.Users.SingleOrDefault(x => x.UserId == RestTestFixture.TestAdminUserId);
            if (existingUser is null)
            {
                ctx.Users.Add(new User
                {
                    UserId = RestTestFixture.TestAdminUserId,
                    UserType = 0,
                    UserSid = RestTestFixture.TestAdminUserId,
                    LoginName = "db-admin",
                    LoginPassword = "db-pass",
                    Alias = "DB Alias",
                    Status = 1,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = RestTestFixture.TestAdminUserId,
                    ModifiedOn = DateTime.UtcNow,
                    ModifiedBy = RestTestFixture.TestAdminUserId,
                    Retired = false
                });
            }
            ctx.UserNotifications.Add(new UserNotification
            {
                NotifyId = Guid.NewGuid(),
                UserId = RestTestFixture.TestAdminUserId,
                DeviceId = "device-1",
                NotifyType = 12,
                Platform = 2,
                MetadataXml = "{\"DeviceInfo\":{\"Id\":\"device-1\",\"Platform\":2},\"Options\":{\"OnReadyPaper\":true}}"
            });
        });

        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/User/Notification/device-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        Assert.Equal("device-1", json.GetProperty("DeviceInfo").GetProperty("Id").GetString());
        Assert.True(json.GetProperty("Options").GetProperty("OnReadyPaper").GetBoolean());
    }

    [Fact]
    public async Task PostNotification_WithOptions_PersistsEnabledTypesAndRemovesDisabledOnes()
    {
        await _factory.SeedAsync(ctx =>
        {
            var existingUser = ctx.Users.SingleOrDefault(x => x.UserId == RestTestFixture.TestAdminUserId);
            if (existingUser is null)
            {
                ctx.Users.Add(new User
                {
                    UserId = RestTestFixture.TestAdminUserId,
                    UserType = 0,
                    UserSid = RestTestFixture.TestAdminUserId,
                    LoginName = "db-admin",
                    LoginPassword = "db-pass",
                    Alias = "DB Alias",
                    Status = 1,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = RestTestFixture.TestAdminUserId,
                    ModifiedOn = DateTime.UtcNow,
                    ModifiedBy = RestTestFixture.TestAdminUserId,
                    Retired = false
                });
            }

            var existingNotification = ctx.UserNotifications
                .SingleOrDefault(x => x.UserId == RestTestFixture.TestAdminUserId && x.DeviceId == "device-2" && x.NotifyType == 1);
            if (existingNotification is null)
            {
                ctx.UserNotifications.Add(new UserNotification
                {
                    NotifyId = Guid.NewGuid(),
                    UserId = RestTestFixture.TestAdminUserId,
                    DeviceId = "device-2",
                    NotifyType = 1,
                    Platform = 1,
                    MetadataXml = "{}"
                });
            }
        });

        using var client = _factory.CreateAuthenticatedClient();

        var payload = new
        {
            DeviceInfo = new { Id = "device-2", Platform = 2 },
            Options = new
            {
                Everyone = false,
                OnReadyPaper = true,
                OnReadyPlate = true
            }
        };

        var response = await client.PostAsJsonAsync("/api/User/Notification/ignored", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var notifications = await _factory.ReadAsync(ctx =>
            ctx.UserNotifications
                .Where(x => x.UserId == RestTestFixture.TestAdminUserId && x.DeviceId == "device-2")
                .OrderBy(x => x.NotifyType)
                .Select(x => new { x.NotifyType, x.Platform, x.MetadataXml })
                .ToListAsync());

        Assert.Equal(2, notifications.Count);
        Assert.Equal(12, notifications[0].NotifyType);
        Assert.Equal(13, notifications[1].NotifyType);
        Assert.All(notifications, x => Assert.Equal(2, x.Platform));
        Assert.All(notifications, x => Assert.Contains("device-2", x.MetadataXml ?? string.Empty, StringComparison.Ordinal));
    }
}

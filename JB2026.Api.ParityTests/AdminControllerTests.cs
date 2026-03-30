using JB2026.Api.Controllers;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JB2026.Api.ParityTests;

public sealed class AdminControllerTests
{
    [Fact]
    public void GetUsers_ReturnsConfiguredUsers()
    {
        var controller = new AdminController(new StubLegacyIdentityService());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = controller.GetUsers();
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IReadOnlyList<JB2026.Api.Models.AdminUserResponse>>(ok.Value);

        Assert.Equal(2, users.Count);
        Assert.Equal("admin", users[0].Username);
        Assert.Equal("operator", users[1].Username);
    }

    private sealed class StubLegacyIdentityService : ILegacyIdentityService
    {
        public LegacyIdentityUser? ValidateCredentials(string username, string password) => null;

        public LegacyIdentityUser? FindByUsername(string username) => null;

        public LegacyIdentityUser? FindByUserId(Guid userId) => null;

        public IReadOnlyList<LegacyIdentityUser> GetUsers()
        {
            return
            [
                new LegacyIdentityUser
                {
                    UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Username = "operator",
                    Password = "x",
                    DisplayName = "Operator",
                    Role = "Operator"
                },
                new LegacyIdentityUser
                {
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Username = "admin",
                    Password = "x",
                    DisplayName = "Administrator",
                    Role = "Admin"
                }
            ];
        }
    }
}
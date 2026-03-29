using System.Net;

namespace JB2026.Rest.Tests;

/// <summary>
/// Tests for TokenCompatibilityController — verifies that legacy /api/Token/* routes
/// issue JWTs for valid credentials and reject invalid or role-mismatched credentials.
/// </summary>
public sealed class TokenCompatibilityControllerTests : IClassFixture<RestTestFixture>
{
    private readonly RestTestFixture _factory;

    public TokenCompatibilityControllerTests(RestTestFixture factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_ValidCredentials_ReturnsOkWithJwtString()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/Token/admin/adminpass");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = (await response.Content.ReadAsStringAsync()).Trim('"');
        // A JWT has exactly two '.' separating header.payload.signature
        Assert.Equal(2, token.Count(c => c == '.'));
    }

    [Fact]
    public async Task Get_InvalidPassword_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/Token/admin/wrongpass");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownUser_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/Token/nobody/anypass");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStaff_AdminCredentials_ReturnsOk()
    {
        // "Admin" role satisfies IsStaffRole ("staff"|"admin")
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/Token/Staff/admin/adminpass");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetClient_AdminCredentials_ReturnsUnauthorized()
    {
        // "Admin" does not satisfy IsClientRole ("customer"|"client")
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/Token/Client/admin/adminpass");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetWithExpiry_ValidCredentials_ReturnsOk()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/Token/admin/adminpass/120");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

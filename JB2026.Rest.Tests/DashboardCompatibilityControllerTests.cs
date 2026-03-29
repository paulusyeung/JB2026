using System.Net;

namespace JB2026.Rest.Tests;

/// <summary>
/// Tests for DashboardCompatibilityController — verifies the auth guard and that
/// role-based filtering produces 200 OK responses (empty arrays when no data is seeded,
/// since the controller queries vwDashboard_* views backed by InMemory EF Core).
/// </summary>
public sealed class DashboardCompatibilityControllerTests : IClassFixture<RestTestFixture>
{
    private readonly RestTestFixture _factory;

    public DashboardCompatibilityControllerTests(RestTestFixture factory)
    {
        _factory = factory;
    }

    // -----------------------------------------------------------------------
    // Auth guard — all four dashboard routes
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("/api/Dashboard/StatJob/Staff/year")]
    [InlineData("/api/Dashboard/StatJob/Average/year")]
    [InlineData("/api/Dashboard/StatSML/Order/year")]
    [InlineData("/api/Dashboard/StatSML/Invoice/year")]
    public async Task AnyDashboardRoute_NoAuth_ReturnsUnauthorized(string route)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // With auth but no User/UserInfo in DB → role defaults to 0 (Guest) → empty result
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetStatJobStaff_AuthenticatedNoUserInDb_ReturnsOkEmptyArray()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/Dashboard/StatJob/Staff/year");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", (await response.Content.ReadAsStringAsync()).Trim());
    }

    [Fact]
    public async Task GetStatJobAverage_AuthenticatedNoUserInDb_ReturnsOkEmptyArray()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/Dashboard/StatJob/Average/year");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", (await response.Content.ReadAsStringAsync()).Trim());
    }

    [Fact]
    public async Task GetStatSmlOrder_AuthenticatedNoUserInDb_ReturnsOkEmptyArray()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/Dashboard/StatSML/Order/year");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", (await response.Content.ReadAsStringAsync()).Trim());
    }

    [Fact]
    public async Task GetStatSmlInvoice_AuthenticatedNoUserInDb_ReturnsOkEmptyArray()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/Dashboard/StatSML/Invoice/year");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", (await response.Content.ReadAsStringAsync()).Trim());
    }

    // -----------------------------------------------------------------------
    // Type parameter variants — "month" and "all" should also return 200
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("year")]
    [InlineData("month")]
    [InlineData("all")]
    public async Task GetStatJobStaff_TypeVariants_ReturnOk(string type)
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/Dashboard/StatJob/Staff/{type}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

using System.Net;

namespace JB2026.Rest.Tests;

public sealed class JobCompatibilityPdfTests : IClassFixture<RestTestFixture>
{
    private readonly RestTestFixture _factory;

    // Seeded in InMemoryJobManagementRepository
    private static readonly Guid KnownOrderId = Guid.Parse("1e84b2e5-3f73-4d60-9d0d-08dc50c00001");

    public JobCompatibilityPdfTests(RestTestFixture factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetJobPdf_UnknownId_ReturnsNotFound()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/Job/pdf/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetJobPdf_KnownId_ReturnsPdfFile()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/Job/pdf/{KnownOrderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var content = await response.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task GetJobPdfOrder_WithNoPictureNoContent_ReturnsPdfFile()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/Job/pdf/job/{KnownOrderId}/true/true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetJobPdfOrder_WithSupplierAndSelectedPds_ReturnsPdfFile()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var supplierId = Guid.NewGuid();
        var response = await client.GetAsync($"/api/Job/pdf/order/{KnownOrderId}/false/{supplierId}/all");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
    }
}

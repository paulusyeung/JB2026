using JB2026.Api.Controllers;
using JB2026.Api.Models;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JB2026.Api.ParityTests;

public sealed class PublicControllerTests
{
    [Fact]
    public void GetContent_ReturnsPublicEntries()
    {
        var controller = new PublicController(new InMemoryPublicContentService());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = controller.GetContent();
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<PublicContentResponse>>(ok.Value);

        Assert.NotEmpty(items);
        Assert.Contains(items, item => item.Slug == "company-profile");
    }
}
using JB2026.Api.Controllers;
using JB2026.Api.Models;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JB2026.Api.ParityTests;

public sealed class HelpControllerTests
{
    [Fact]
    public void GetArticles_ReturnsHelpArticles()
    {
        var controller = new HelpController(new InMemoryHelpContentService());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = controller.GetArticles();
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<HelpArticleResponse>>(ok.Value);

        Assert.NotEmpty(items);
        Assert.Contains(items, item => item.ArticleId == "getting-started");
    }
}
using JB2026.Api.Models;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/help")]
public sealed class HelpController : ControllerBase
{
    private readonly IHelpContentService _helpContentService;

    public HelpController(IHelpContentService helpContentService)
    {
        _helpContentService = helpContentService;
    }

    [HttpGet("articles")]
    [ProducesResponseType(typeof(IReadOnlyList<HelpArticleResponse>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<HelpArticleResponse>> GetArticles()
    {
        return Ok(_helpContentService.GetArticles());
    }
}
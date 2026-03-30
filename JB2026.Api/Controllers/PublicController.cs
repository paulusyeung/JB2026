using JB2026.Api.Models;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/public")]
public sealed class PublicController : ControllerBase
{
    private readonly IPublicContentService _publicContentService;

    public PublicController(IPublicContentService publicContentService)
    {
        _publicContentService = publicContentService;
    }

    [HttpGet("content")]
    [ProducesResponseType(typeof(IReadOnlyList<PublicContentResponse>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<PublicContentResponse>> GetContent()
    {
        return Ok(_publicContentService.GetContent());
    }
}
using JB2026.Api.Models;
using JB2026.Api.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/config")]
public sealed class ConfigController : ControllerBase
{
    private readonly IOptions<PaperlessNgxOptions> _paperlessNgxOptions;

    public ConfigController(IOptions<PaperlessNgxOptions> paperlessNgxOptions)
    {
        _paperlessNgxOptions = paperlessNgxOptions;
    }

    [HttpGet("paperless-ngx")]
    [ProducesResponseType(typeof(PaperlessNgxConfigResponse), StatusCodes.Status200OK)]
    public ActionResult<PaperlessNgxConfigResponse> GetPaperlessNgxConfig()
    {
        var cfg = _paperlessNgxOptions.Value;
        var configured = !string.IsNullOrWhiteSpace(cfg.BaseUrl)
            && !string.IsNullOrWhiteSpace(cfg.ApiToken)
            && !string.IsNullOrWhiteSpace(cfg.DefaultUser);

        return Ok(new PaperlessNgxConfigResponse { Configured = configured });
    }
}
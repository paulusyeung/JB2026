using JB2026.WebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JB2026.WebApp.Controllers;

[ApiController]
[AllowAnonymous]
[Route("ui/feature-flags")]
public sealed class UiFeatureFlagsController : ControllerBase
{
    private readonly IUiFeatureFlagStore _featureFlagStore;

    public UiFeatureFlagsController(IUiFeatureFlagStore featureFlagStore)
    {
        _featureFlagStore = featureFlagStore;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<UiSliceFlagSnapshot>> GetFlags()
    {
        return Ok(_featureFlagStore.GetCurrentSlices());
    }
}
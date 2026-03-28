using JB2026.ApiPilot.Models;
using JB2026.ApiPilot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JB2026.ApiPilot.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/jobs")]
public sealed class JobsController : ControllerBase
{
    private readonly LegacyJobRepository _repository;

    public JobsController(LegacyJobRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("range")]
    [ProducesResponseType(typeof(IReadOnlyList<JobListItem>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<JobListItem>> GetRange([FromQuery] DateOnly startOn, [FromQuery] int days)
    {
        if (days <= 0 || days > 31)
        {
            return ValidationProblem("Days must be between 1 and 31.");
        }

        return Ok(_repository.GetRange(startOn, days));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<JobDetail> GetById(Guid id)
    {
        var job = _repository.GetById(id);
        return job is null ? NotFound() : Ok(job);
    }
}
using JB2026.Api.Models;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/jobs")]
public sealed class JobsController : ControllerBase
{
    private readonly IJobManagementRepository _repository;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IJobManagementRepository repository, ILogger<JobsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet("range")]
    [ProducesResponseType(typeof(IReadOnlyList<JobListItemResponse>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<JobListItemResponse>> GetRange([FromQuery] DateOnly startOn, [FromQuery] int days)
    {
        if (days is <= 0 or > 31)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(days)] = ["Days must be between 1 and 31."]
            }));
        }

        var jobs = _repository.GetRange(startOn, days);
        _logger.LogInformation("Returned {Count} jobs for range query starting on {StartOn} with {Days} days", jobs.Count, startOn, days);
        return Ok(jobs);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<JobDetailResponse> GetById(Guid id)
    {
        var job = _repository.GetJobDetail(id);
        if (job is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Job not found",
                Detail = $"No job exists for order id '{id}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(job);
    }

    [HttpGet("{id:guid}/details")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<string>> GetDetails(Guid id)
    {
        var styleTitles = _repository.GetStyleTitles(id);
        if (styleTitles.Count == 0)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Job details not found",
                Detail = $"No style titles exist for order id '{id}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(styleTitles);
    }
}

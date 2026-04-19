using JB2026.Api.Models;
using JB2026.Api.Options;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/job-orders")]
public sealed class JobOrdersController : ControllerBase
{
    private readonly IJobManagementRepository _repository;
    private readonly ICurrentUserProfileService _currentUserProfileService;
    private readonly JobListOptions _jobListOptions;
    private readonly ILogger<JobOrdersController> _logger;

    public JobOrdersController(
        IJobManagementRepository repository,
        ICurrentUserProfileService currentUserProfileService,
        IOptions<JobListOptions> jobListOptions,
        ILogger<JobOrdersController> logger)
    {
        _repository = repository;
        _currentUserProfileService = currentUserProfileService;
        _jobListOptions = jobListOptions.Value;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<JobOrderResponse>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<JobOrderResponse>> GetAll(
        [FromQuery] int? take,
        [FromQuery] string? lookup,
        [FromQuery] int? commonQuery,
        [FromQuery] string? startsWith,
        [FromQuery] string? listType,
        [FromQuery] DateOnly? startOn,
        [FromQuery] DateOnly? endOn)
    {
        var isJobList = string.Equals(listType, "job", StringComparison.OrdinalIgnoreCase);
        var isOrderList = string.Equals(listType, "order", StringComparison.OrdinalIgnoreCase);
        var hasFilters = !string.IsNullOrWhiteSpace(lookup) 
            || commonQuery.GetValueOrDefault() > 0 
            || !string.IsNullOrWhiteSpace(startsWith)
            || startOn.HasValue
            || endOn.HasValue;
        var defaultTake = hasFilters ? _jobListOptions.FilteredTake : _jobListOptions.InitialTake;
        var maxTake = Math.Max(1, _jobListOptions.MaxTake);
        var requestedTake = take.GetValueOrDefault(defaultTake);
        var jobListTake = Math.Clamp(requestedTake, 1, maxTake);
        var orders = isJobList
            ? _repository.GetJobList(lookup, commonQuery.GetValueOrDefault(), startsWith, jobListTake, startOn, endOn)
            : isOrderList
            ? _repository.GetOrderList(lookup, commonQuery.GetValueOrDefault(), startsWith, jobListTake, startOn, endOn)
            : hasFilters
            ? _repository.GetOrderList(lookup, commonQuery.GetValueOrDefault(), startsWith, jobListTake, startOn, endOn)
            : _repository.GetJobOrders(requestedTake);

        return Ok(orders);
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(IReadOnlyList<JobStatsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<JobStatsResponse>> GetStats(
        [FromQuery] DateOnly? startOn,
        [FromQuery] DateOnly? endOn)
    {
        if (startOn.HasValue && endOn.HasValue && startOn.Value > endOn.Value)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(startOn)] = ["Start date must be on or before end date."],
                [nameof(endOn)] = ["End date must be on or after start date."],
            }));
        }

        var rows = _repository.GetJobStats(startOn, endOn);
        return Ok(rows);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<JobOrderResponse> GetById(Guid id)
    {
        var order = _repository.GetJobOrder(id);
        if (order is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Job order not found",
                Detail = $"No job order exists for order id '{id}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(order);
    }

    [HttpPost]
    [ProducesResponseType(typeof(JobOrderResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<JobOrderResponse>> Create([FromBody] CreateJobOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.RequiredOn < request.OrderedOn)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.RequiredOn)] = ["RequiredOn must be on or after OrderedOn."]
            }));
        }

        var actor = GetActor();
        var order = await _repository.CreateJobOrder(request, actor);
        _logger.LogInformation("Created job order {OrderId} by {Actor}", order.OrderId, actor);

        return CreatedAtAction(nameof(GetById), new { id = order.OrderId }, order);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(JobOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobOrderResponse>> Update(Guid id, [FromBody] UpdateJobOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var actor = GetActor();
    var order = await _repository.UpdateJobOrder(id, request, actor);
        if (order is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Job order not found",
                Detail = $"No job order exists for order id '{id}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        _logger.LogInformation("Updated job order {OrderId} by {Actor}", id, actor);
        return Ok(order);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(JobOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobOrderResponse>> Delete(Guid id)
    {
        var order = await _repository.DeleteJobOrder(id);
        if (order is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Job order not found",
                Detail = $"No job order exists for order id '{id}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        _logger.LogInformation("Deleted job order {OrderId}", id);
        return Ok(order);
    }

    private string GetActor()
    {
        return _currentUserProfileService.GetCurrentUser()?.Username ?? User.Identity?.Name ?? "system";
    }
}

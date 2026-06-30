using JB2026.Api.Models;
using JB2026.Api.Options;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    private readonly LegacyFilesOptions _legacyFiles;
    private readonly ILogger<JobOrdersController> _logger;

    public JobOrdersController(
        IJobManagementRepository repository,
        ICurrentUserProfileService currentUserProfileService,
        IOptions<JobListOptions> jobListOptions,
        IOptions<LegacyFilesOptions> legacyFiles,
        ILogger<JobOrdersController> logger)
    {
        _repository = repository;
        _currentUserProfileService = currentUserProfileService;
        _jobListOptions = jobListOptions.Value;
        _legacyFiles = legacyFiles.Value;
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
        [FromQuery] DateOnly? endOn,
        [FromQuery] int? status)
    {
        var isJobList = string.Equals(listType, "job", StringComparison.OrdinalIgnoreCase);
        var isOrderList = string.Equals(listType, "order", StringComparison.OrdinalIgnoreCase);
        var hasFilters = !string.IsNullOrWhiteSpace(lookup) 
            || commonQuery.GetValueOrDefault() > 0 
            || status.HasValue
            || !string.IsNullOrWhiteSpace(startsWith)
            || startOn.HasValue
            || endOn.HasValue;
        var defaultTake = hasFilters ? _jobListOptions.FilteredTake : _jobListOptions.InitialTake;
        var maxTake = Math.Max(1, _jobListOptions.MaxTake);
        var requestedTake = take.GetValueOrDefault(defaultTake);
        var jobListTake = Math.Clamp(requestedTake, 1, maxTake);
        var orders = isJobList
            ? _repository.GetJobList(lookup, commonQuery.GetValueOrDefault(), startsWith, jobListTake, startOn, endOn, status)
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
        // Load job detail before deleting so we have attachment info for file cleanup
        var jobDetail = _repository.GetJobDetail(id);
        if (jobDetail is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Job order not found",
                Detail = $"No job order exists for order id '{id}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        // Delete DB records: workflow forms, workflows, attachments, order, and rebuild sibling job numbers
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

        // Delete physical attachment files (best-effort: log warnings, do not fail the request)
        if (jobDetail.Attachments.Count > 0)
        {
            DeleteAttachmentFiles(id, jobDetail);
        }

        _logger.LogInformation(
            "Deleted job order {OrderId} ({WorkflowCount} workflows, {AttachmentCount} attachments)",
            id, jobDetail.StyleTitles.Length, jobDetail.Attachments.Count);
        return Ok(order);
    }

    [HttpGet("~/api/v2/order-types/{orderType}/workflow-attributes")]
    [ProducesResponseType(typeof(OrderTypeWorkflowAttributeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderTypeWorkflowAttributeResponse>> GetWorkflowAttributes(
        [FromServices] JB5LegacyReadContext readContext,
        int orderType,
        CancellationToken cancellationToken = default)
    {
        if (orderType is < 0 or > 3)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(orderType)] = ["OrderType must be between 0 and 3."]
            }));
        }

        var attributes = await readContext.Z_OrderTypeWorkflows
            .AsNoTracking()
            .Where(mapping => mapping.OrderType == orderType && mapping.WorkflowId.HasValue)
            .Where(mapping => mapping.Workflow!.Z_WorkflowForms.Any())
            .OrderBy(mapping => mapping.WorkIndex)
            .Include(mapping => mapping.Workflow)
            .Select(mapping => new OrderTypeWorkflowAttributeItemResponse
            {
                WorkIndex = mapping.WorkIndex,
                WorkflowName = mapping.Workflow!.WorkflowName ?? string.Empty,
                Options = mapping.Workflow.WorkTitle != null
                    ? mapping.Workflow.WorkTitle.Split(';', StringSplitOptions.None).ToList()
                    : new List<string>(),
            })
            .ToListAsync(cancellationToken);

        return Ok(new OrderTypeWorkflowAttributeResponse
        {
            WorkflowAttributes = attributes,
        });
    }

    private void DeleteAttachmentFiles(Guid orderId, JobDetailResponse jobDetail)
    {
        var baseOrderNumber = ExtractBaseOrderNumber(jobDetail.OrderNumber);
        var rootCandidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(_legacyFiles.FileAgentRoot))
            rootCandidates.Add(_legacyFiles.FileAgentRoot);
        if (!string.IsNullOrWhiteSpace(_legacyFiles.InBox))
            rootCandidates.Add(_legacyFiles.InBox);

        foreach (var attachment in jobDetail.Attachments)
        {
            if (string.IsNullOrWhiteSpace(attachment.FileName))
                continue;

            var probes = new List<string>();
            var typeFolder = attachment.AttachmentType.ToString();

            foreach (var root in rootCandidates)
            {
                var orderDir = Path.Combine(root, baseOrderNumber);
                probes.Add(Path.Combine(orderDir, typeFolder, attachment.FileName));
                probes.Add(Path.Combine(orderDir, attachment.FileName));
            }

            if (!string.IsNullOrWhiteSpace(_legacyFiles.CloudDiskRoot))
            {
                probes.Add(Path.Combine(_legacyFiles.CloudDiskRoot, "uploads", orderId.ToString("N"), attachment.FileName));
            }

            var deleted = false;
            foreach (var path in probes)
            {
                try
                {
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                        deleted = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to delete attachment file {FilePath} for attachment {AttachmentId} during delete of job order {OrderId}",
                        path, attachment.AttachmentId, orderId);
                }
            }

            if (!deleted)
            {
                _logger.LogDebug(
                    "No physical file found for attachment {AttachmentId} (file: {FileName}) of job order {OrderId}; skipping file cleanup",
                    attachment.AttachmentId, attachment.FileName, orderId);
            }
        }
    }

    private static string ExtractBaseOrderNumber(string compositeOrderNumber)
    {
        var dashIndex = compositeOrderNumber.LastIndexOf('-');
        if (dashIndex > 0 && int.TryParse(compositeOrderNumber[(dashIndex + 1)..], out _))
        {
            return compositeOrderNumber[..dashIndex];
        }
        return compositeOrderNumber;
    }

    private string GetActor()
    {
        return _currentUserProfileService.GetCurrentUser()?.Username ?? User.Identity?.Name ?? "system";
    }
}

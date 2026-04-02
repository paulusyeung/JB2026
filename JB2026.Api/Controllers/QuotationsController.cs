using JB2026.Api.Models;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/quotations")]
public sealed class QuotationsController : ControllerBase
{
    private readonly IQuotationRepository _repository;
    private readonly ICurrentUserProfileService _currentUserProfileService;
    private readonly ILogger<QuotationsController> _logger;

    public QuotationsController(
        IQuotationRepository repository,
        ICurrentUserProfileService currentUserProfileService,
        ILogger<QuotationsController> logger)
    {
        _repository = repository;
        _currentUserProfileService = currentUserProfileService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<QuotationListItemResponse>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<QuotationListItemResponse>> GetRange([FromQuery] DateOnly startOn, [FromQuery] int days)
    {
        if (days is <= 0 or > 31)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(days)] = ["Days must be between 1 and 31."]
            }));
        }

        var quotations = _repository.GetRange(startOn, days);
        _logger.LogInformation("Returned {Count} quotations for range query starting on {StartOn} with {Days} days", quotations.Count, startOn, days);
        return Ok(quotations);
    }

    [HttpGet("search/{keyword}")]
    [ProducesResponseType(typeof(IReadOnlyList<QuotationListItemResponse>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<QuotationListItemResponse>> Search(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword) || keyword.Trim().Length < 3)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(keyword)] = ["Keyword must contain at least 3 characters."]
            }));
        }

        var quotations = _repository.Search(keyword);
        return Ok(quotations);
    }

    [HttpGet("{id:guid}/pdf")]
    public IActionResult GetPdf(Guid id)
    {
        var pdf = _repository.GetPdf(id);
        if (pdf is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Quotation not found",
                Detail = $"No quotation exists for header id '{id}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return File(pdf.Value.Content, "application/pdf", pdf.Value.FileName);
    }

    [HttpPost]
    [ProducesResponseType(typeof(QuotationListItemResponse), StatusCodes.Status201Created)]
    public ActionResult<QuotationListItemResponse> Create([FromBody] UpsertQuotationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var actor = GetActor();
        var quotation = _repository.Create(request, actor);
        _logger.LogInformation("Created quotation {HeaderId} by {Actor}", quotation.HeaderId, actor);

        return CreatedAtAction(nameof(GetRange), quotation);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(QuotationListItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<QuotationListItemResponse> Update(Guid id, [FromBody] UpsertQuotationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var actor = GetActor();
        var quotation = _repository.Update(id, request, actor);
        if (quotation is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Quotation not found",
                Detail = $"No quotation exists for header id '{id}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        _logger.LogInformation("Updated quotation {HeaderId} by {Actor}", id, actor);
        return Ok(quotation);
    }

    private string GetActor()
    {
        return _currentUserProfileService.GetCurrentUser()?.Username ?? User.Identity?.Name ?? "system";
    }
}

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
    private readonly ICurrentUserProfileService _currentUserProfileService;
    private readonly ILogger<JobsController> _logger;
    private readonly IConfiguration _configuration;

    public JobsController(
        IJobManagementRepository repository,
        ICurrentUserProfileService currentUserProfileService,
        ILogger<JobsController> logger,
        IConfiguration configuration)
    {
        _repository = repository;
        _currentUserProfileService = currentUserProfileService;
        _logger = logger;
        _configuration = configuration;
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

    [HttpPost]
    [ProducesResponseType(typeof(JobOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
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
        _logger.LogInformation("Created job {OrderId} by {Actor}", order.OrderId, actor);

        return Created($"/api/v2/jobs/{order.OrderId}", order);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(JobOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
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
                Title = "Job not found",
                Detail = $"No job exists for order id '{id}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        _logger.LogInformation("Updated job {OrderId} by {Actor}", id, actor);
        return Ok(order);
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

    [HttpGet("/api/Job/preview/{orderId:guid}/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult GetPreview(Guid orderId, string fileName, [FromQuery] string? attachmentType)
    {
        var job = _repository.GetJobDetail(orderId);
        if (job is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Job not found",
                Detail = $"No job exists for order id '{orderId}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid file name",
                Detail = "File name cannot be empty.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var baseOrderNumber = ExtractBaseOrderNumber(job.OrderNumber);
        var locatedPath = LocatePreviewFile(orderId, baseOrderNumber, job.OrderedOn, safeFileName, attachmentType);
        if (locatedPath is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Preview not found",
                Detail = $"No preview file found for '{safeFileName}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return PhysicalFile(locatedPath, GetContentType(locatedPath));
    }

    private string GetActor()
    {
        return _currentUserProfileService.GetCurrentUser()?.Username ?? User.Identity?.Name ?? "system";
    }

    private string? LocatePreviewFile(Guid orderId, string orderNumber, DateTime orderedOn, string fileName, string? attachmentType)
    {
        var probes = new List<string>();
        var candidateFileNames = BuildPreviewFileNameCandidates(fileName);
        var normalizedAttachmentFolder = NormalizeFolderSegment(attachmentType);

        var fileAgentRoot = _configuration["LegacyFiles:FileAgentRoot"];
        foreach (var rootCandidate in ExpandRootCandidates(fileAgentRoot))
        {
            var legacyOrderFolder = Path.Combine(rootCandidate, orderNumber);
            probes.Add(legacyOrderFolder);

            var migratedOrderFolder = Path.Combine(rootCandidate, "JB5", orderedOn.ToString("yyyy"), orderedOn.ToString("MM"), orderNumber);
            probes.Add(migratedOrderFolder);

            if (!string.IsNullOrWhiteSpace(normalizedAttachmentFolder))
            {
                probes.Add(Path.Combine(legacyOrderFolder, normalizedAttachmentFolder));
                probes.Add(Path.Combine(migratedOrderFolder, normalizedAttachmentFolder));
            }
        }

        var cloudDiskRoot = _configuration["LegacyFiles:CloudDiskRoot"];
        foreach (var cloudRoot in ExpandRootCandidates(cloudDiskRoot))
        {
            probes.Add(Path.Combine(cloudRoot, "uploads", orderId.ToString("N")));
        }

        foreach (var folder in probes.Distinct())
        {
            foreach (var candidate in candidateFileNames)
            {
                if (!Directory.Exists(folder))
                {
                    continue;
                }

                var directPath = Path.Combine(folder, candidate);
                if (System.IO.File.Exists(directPath))
                {
                    return directPath;
                }

                try
                {
                    var recursivePath = FindRecursivePathCaseInsensitive(folder, candidate);
                    if (!string.IsNullOrWhiteSpace(recursivePath))
                    {
                        return recursivePath;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Ignore unreadable legacy subfolders and continue probing.
                }
            }
        }

        return null;
    }

    private static IReadOnlyList<string> BuildPreviewFileNameCandidates(string fileName)
    {
        var safe = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safe))
        {
            return Array.Empty<string>();
        }

        var candidates = new List<string> { safe };
        var normalized = safe.ToLowerInvariant();

        if (normalized.EndsWith(".pdf"))
        {
            candidates.Insert(0, $"{safe}.jpg");
            candidates.Insert(1, $"{safe[..^4]}.jpg");
            candidates.Add($"{safe[..^4]}.jpeg");
            candidates.Add($"{safe[..^4]}.png");
            candidates.Add($"{safe[..^4]}.webp");
        }

        if (normalized.EndsWith(".pdf.jpg"))
        {
            candidates.Add(safe[..^4]);
        }

        return candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string NormalizeFolderSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace("\\", string.Empty).Replace("/", string.Empty).Trim();
        return normalized.All(char.IsDigit) ? normalized : string.Empty;
    }

    private static IReadOnlyList<string> ExpandRootCandidates(string? configuredRoot)
    {
        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            return Array.Empty<string>();
        }

        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            configuredRoot
        };

        if (configuredRoot.StartsWith("\\\\", StringComparison.Ordinal))
        {
            var parts = configuredRoot.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var server = parts[0];
                var share = parts[1];
                var tail = parts.Skip(2).ToArray();

                roots.Add('/' + string.Join('/', parts));

                var mntPath = Path.Combine(new[] { "/mnt", server, share }.Concat(tail).ToArray());
                roots.Add(mntPath);

                var mediaPath = Path.Combine(new[] { "/media", server, share }.Concat(tail).ToArray());
                roots.Add(mediaPath);
            }
        }

        return roots.ToList();
    }

    private static string ExtractBaseOrderNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        var lastDash = trimmed.LastIndexOf('-');
        if (lastDash > 0)
        {
            var suffix = trimmed[(lastDash + 1)..];
            if (suffix.All(char.IsDigit))
            {
                return trimmed[..lastDash];
            }
        }

        return trimmed;
    }

    private static string? FindRecursivePathCaseInsensitive(string folder, string candidate)
    {
        var targetName = Path.GetFileName(candidate);
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        return Directory
            .EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .FirstOrDefault(path => string.Equals(Path.GetFileName(path), targetName, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetContentType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}

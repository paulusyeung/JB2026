using JB2026.Api.Models;
using JB2026.Api.Options;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/jobs")]
public sealed class JobsController : ControllerBase
{
    private const long MaxUploadBytes = 25 * 1024 * 1024;

    private readonly IJobManagementRepository _repository;
    private readonly IJobAttachmentStoredProcedureGateway _jobAttachmentGateway;
    private readonly JB5LegacyReadContext _readContext;
    private readonly ICurrentUserProfileService _currentUserProfileService;
    private readonly ILogger<JobsController> _logger;
    private readonly LegacyFilesOptions _legacyFiles;
    private readonly IJobOrderPrintComposer _jobOrderPrintComposer;
    private readonly IJobOrderPdfRenderer _jobOrderPdfRenderer;

    public JobsController(
        IJobManagementRepository repository,
        IJobAttachmentStoredProcedureGateway jobAttachmentGateway,
        JB5LegacyReadContext readContext,
        ICurrentUserProfileService currentUserProfileService,
        ILogger<JobsController> logger,
        IOptions<LegacyFilesOptions> legacyFiles,
        IJobOrderPrintComposer jobOrderPrintComposer,
        IJobOrderPdfRenderer jobOrderPdfRenderer)
    {
        _repository = repository;
        _jobAttachmentGateway = jobAttachmentGateway;
        _readContext = readContext;
        _currentUserProfileService = currentUserProfileService;
        _logger = logger;
        _legacyFiles = legacyFiles.Value;
        _jobOrderPrintComposer = jobOrderPrintComposer;
        _jobOrderPdfRenderer = jobOrderPdfRenderer;
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

    [HttpPost("{id:guid}/print")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PrintJobOrder(Guid id, [FromBody] JobOrderPrintRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await _jobOrderPrintComposer.ComposeAsync(id, request, cancellationToken);
            if (document is null)
            {
                return NotFound();
            }

            var pdfContent = _jobOrderPdfRenderer.Render(document);
            var safeOrderNumber = string.IsNullOrWhiteSpace(document.OrderNumber)
                ? id.ToString("N")
                : document.OrderNumber;

            return File(pdfContent, "application/pdf", $"job-order-{safeOrderNumber}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate job-order print PDF for order {OrderId} with options {@PrintOptions}", id, request);
            return Problem(
                title: "Unable to generate job-order print PDF",
                detail: "An unexpected error occurred while generating the job order print report.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("{id:guid}/attachments")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    public async Task<ActionResult> UploadAttachments(
        Guid id,
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken = default)
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

        if (files.Count == 0)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(files)] = ["At least one file is required."]
            }));
        }

        foreach (var file in files)
        {
            if (file.Length == 0)
            {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    [nameof(files)] = [$"File '{file.FileName}' is empty."]
                }));
            }

            if (file.Length > MaxUploadBytes)
            {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    [nameof(files)] = [$"File '{file.FileName}' exceeds the 25MB upload limit."]
                }));
            }
        }

        const int attachmentType = 0;
        var baseOrderNumber = ExtractBaseOrderNumber(job.OrderNumber);
        var attachmentDir = EnsureJobAttachmentDirectory(baseOrderNumber, attachmentType);
        if (string.IsNullOrWhiteSpace(attachmentDir))
        {
            return Problem(
                title: "Attachment storage unavailable",
                detail: "Legacy file storage path for job attachments is not configured.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var maxIndex = await _readContext.JobAttachments
            .Where(attachment => attachment.OrderId == id)
            .Select(attachment => (int?)attachment.AttachmentIndex)
            .MaxAsync(cancellationToken);
        var nextIndex = maxIndex.HasValue ? maxIndex.Value + 1 : 0;

        foreach (var file in files)
        {
            var safeOriginal = Path.GetFileName(file.FileName);
            var fileName = EnsureUniqueAttachmentFileName(attachmentDir, safeOriginal);
            var fullPath = Path.Combine(attachmentDir, fileName);

            await using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            await _jobAttachmentGateway.InsertAsync(new CreateJobAttachmentStoredProcedureRequest(
                OrderId: id,
                AttachmentType: attachmentType,
                AttachmentIndex: nextIndex++,
                OriginalFileName: fileName),
                cancellationToken);
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}/attachments")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAttachments(
        Guid id,
        [FromBody] JobAttachmentDeleteRequest request,
        CancellationToken cancellationToken = default)
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

        if (request.AttachmentIds.Count == 0)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.AttachmentIds)] = ["At least one attachment id is required."]
            }));
        }

        var baseOrderNumber = ExtractBaseOrderNumber(job.OrderNumber);
        foreach (var attachmentId in request.AttachmentIds.Distinct())
        {
            var record = await _jobAttachmentGateway.SelectAsync(attachmentId, cancellationToken);
            if (record is null || record.OrderId != id)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(record.OriginalFileName))
            {
                var filePath = LocatePreviewFile(
                    id,
                    baseOrderNumber,
                    job.OrderedOn,
                    record.OriginalFileName,
                    record.AttachmentType.ToString());

                if (!string.IsNullOrWhiteSpace(filePath) && System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            await _jobAttachmentGateway.DeleteAsync(attachmentId, cancellationToken);
        }

        return NoContent();
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

    private string EnsureUniqueAttachmentFileName(string attachmentDir, string fileName)
    {
        var safeBase = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(safeBase))
        {
            safeBase = "attachment";
        }

        var extension = Path.GetExtension(fileName);
        var candidate = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = $"{safeBase}{extension}";
        }

        var counter = 1;
        while (System.IO.File.Exists(Path.Combine(attachmentDir, candidate)))
        {
            candidate = $"{safeBase} ({counter}){extension}";
            counter++;
        }

        return candidate;
    }

    private string? EnsureJobAttachmentDirectory(string orderNumber, int? attachmentType = null)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            return null;
        }

        var candidates = new List<string>();
        candidates.AddRange(ExpandRootCandidates(_legacyFiles.FileAgentRoot));
        candidates.AddRange(ExpandRootCandidates(_legacyFiles.InBox));

        var root = candidates.FirstOrDefault(Directory.Exists)
            ?? candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));

        if (string.IsNullOrWhiteSpace(root))
        {
            return null;
        }

        var attachmentDir = Path.Combine(root, orderNumber);
        if (attachmentType.HasValue)
        {
            attachmentDir = Path.Combine(attachmentDir, attachmentType.Value.ToString());
        }

        Directory.CreateDirectory(attachmentDir);
        return attachmentDir;
    }

    private string? LocatePreviewFile(Guid orderId, string orderNumber, DateTime orderedOn, string fileName, string? attachmentType)
    {
        var probes = new List<string>();
        var candidateFileNames = BuildPreviewFileNameCandidates(fileName);
        var normalizedAttachmentFolder = NormalizeFolderSegment(attachmentType);

        foreach (var rootCandidate in GetLegacyPreviewRoots())
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

        var cloudDiskRoot = _legacyFiles.CloudDiskRoot;
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

    private IReadOnlyList<string> GetLegacyPreviewRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var configuredRoot in new[] { _legacyFiles.FileAgentRoot, _legacyFiles.InBox })
        {
            foreach (var expandedRoot in ExpandRootCandidates(configuredRoot))
            {
                roots.Add(expandedRoot);
            }
        }

        return roots.ToList();
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

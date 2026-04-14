using System.Security.Claims;
using System.Text;
using JB2026.Api.Models;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Rest.Controllers;

[ApiController]
[Authorize]
public sealed class JobCompatibilityController : ControllerBase
{
    private const int UserRoleManager = 3;
    private const int UserRoleAdmin = 4;

    private readonly IJobManagementRepository _repository;
    private readonly JB5LegacyReadContext _readContext;
    private readonly IConfiguration _configuration;

    public JobCompatibilityController(
        IJobManagementRepository repository,
        JB5LegacyReadContext readContext,
        IConfiguration configuration)
    {
        _repository = repository;
        _readContext = readContext;
        _configuration = configuration;
    }

    [HttpGet("api/Job/{id:guid}")]
    public IActionResult GetJob(Guid id)
    {
        var job = _repository.GetJobDetail(id);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpGet("api/Job/details/{id:guid}")]
    public IActionResult GetJobDetails(Guid id)
    {
        var details = _repository.GetStyleTitles(id);
        return Ok(details);
    }

    [HttpGet("api/Job/{starton:datetime}/{days:int}")]
    public IActionResult GetJob(DateTime starton, int days)
    {
        if (days is <= 0 or > 366)
        {
            return BadRequest("days must be between 1 and 366");
        }

        var result = _repository.GetRange(DateOnly.FromDateTime(starton), days);
        return Ok(result);
    }

    [HttpGet("api/Job/ByMonth/{id:int}/{date:datetime}")]
    public IActionResult GetJobByMonth(int id, DateTime date)
    {
        _ = id;

        var monthStart = new DateOnly(date.Year, date.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
        var result = _repository.GetRange(monthStart, daysInMonth);
        return Ok(result);
    }

    [HttpGet("api/Job/Shipping")]
    public async Task<IActionResult> GetJobShipping(CancellationToken cancellationToken)
    {
        var (userRole, alias) = await GetCurrentAccessAsync(cancellationToken);

        var upper = DateTime.Now.AddDays(3);
        var lower = DateTime.Now.AddDays(-3);
        var query = _readContext.vwJobLists
            .AsNoTracking()
            .Where(x => x.RequiredOn.HasValue && x.RequiredOn.Value <= upper && x.RequiredOn.Value >= lower);

        if (userRole == UserRoleManager && !string.IsNullOrWhiteSpace(alias))
        {
            query = query.Where(x => x.OrderedBy == alias);
        }
        else if (userRole != UserRoleAdmin)
        {
            query = query.Where(x => false);
        }

        var list = await query
            .OrderBy(x => x.RequiredOn)
            .ThenBy(x => x.OrderNumber)
            .Select(p => new
            {
                p.CompletedOn,
                p.CreatedBy,
                p.CreatedOn,
                p.CustomerName,
                p.CustomerRef,
                p.InvoiceAmount,
                p.InvoiceRef,
                OrderNumber = (p.OrderNumber ?? string.Empty) + "-" + (p.JobNumber ?? 0),
                p.ModifiedBy,
                p.ModifiedOn,
                p.OrderedBy,
                p.OrderedOn,
                p.OrderId,
                p.OrderTitle,
                p.OrderType,
                p.OutputRef,
                p.PaymentTerms,
                p.ProductCode,
                p.ProductStyle,
                p.Qty,
                p.Remarks,
                p.RequiredOn,
                p.Retired,
                p.RetiredBy,
                p.RetiredOn,
                p.SONumber,
                p.Status
            })
            .ToListAsync(cancellationToken);

        return Ok(list);
    }

    [HttpGet("api/Job/Keyword/{keyword}")]
    public IActionResult GetJobByKeyword(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Ok(Array.Empty<object>());
        }

        var buffer = _repository.GetRange(DateOnly.FromDateTime(DateTime.Today.AddDays(-180)), 180);
        var result = buffer.Where(job =>
                Contains(job.OrderNumber, keyword)
                || Contains(job.CustomerName, keyword)
                || Contains(job.CustomerRef, keyword)
                || Contains(job.OrderTitle, keyword))
            .ToList();

        return Ok(result);
    }

    [HttpGet("api/Job/pdf/{id:guid}")]
    public IActionResult GetJobPdf(Guid id)
    {
        var job = _repository.GetJobDetail(id);
        if (job is null)
        {
            return NotFound();
        }

        return BuildPdfResponse(job, nopicture: false, nocontent: false, supplierid: null, selectedpds: null);
    }

    [HttpGet("api/Job/pdf/job/{jobid:guid}/{nopicture:bool}/{nocontent:bool}")]
    public IActionResult GetJobPdfOrder(Guid jobid, bool nopicture, bool nocontent)
    {
        var job = _repository.GetJobDetail(jobid);
        if (job is null)
        {
            return NotFound();
        }

        return BuildPdfResponse(job, nopicture, nocontent, supplierid: null, selectedpds: null);
    }

    [HttpGet("api/Job/pdf/order/{jobid:guid}/{nopicture:bool}/{supplierid:guid}/{selectedpds}")]
    public IActionResult GetJobPdfOrder(Guid jobid, bool nopicture, Guid supplierid, string selectedpds)
    {
        var job = _repository.GetJobDetail(jobid);
        if (job is null)
        {
            return NotFound();
        }

        return BuildPdfResponse(job, nopicture, nocontent: false, supplierid, selectedpds);
    }

    [HttpGet("api/Job/preview/{orderId:guid}/{fileName}")]
    public async Task<IActionResult> GetJobPreview(
        Guid orderId,
        string fileName,
        [FromQuery] string? attachmentType,
        CancellationToken cancellationToken)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            return BadRequest("Invalid file name.");
        }

        var order = await _readContext.JobOrders
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);

        if (order is null)
        {
            return NotFound();
        }

        var orderNumber = order.OrderNumber ?? string.Empty;
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            return NotFound();
        }

        var locatedPath = LocatePreviewFile(orderId, orderNumber, order.JobNumber ?? 0, order.OrderedOn, safeFileName, attachmentType);
        if (locatedPath is null)
        {
            return NotFound();
        }

        return PhysicalFile(locatedPath, GetContentType(locatedPath));
    }

    private static FileContentResult BuildPdfResponse(
        JobDetailResponse job,
        bool nopicture,
        bool nocontent,
        Guid? supplierid,
        string? selectedpds)
    {
        var (content, fileName) = CreateCompatibilityPdf(job, nopicture, nocontent, supplierid, selectedpds);
        return new FileContentResult(content, "application/pdf")
        {
            FileDownloadName = fileName
        };
    }

    private static (byte[] Content, string FileName) CreateCompatibilityPdf(
        JobDetailResponse job,
        bool nopicture,
        bool nocontent,
        Guid? supplierid,
        string? selectedpds)
    {
        var contentStream = GetPdfContentStream(job, nopicture, nocontent, supplierid, selectedpds);
        var lines = new[]
        {
            "%PDF-1.4",
            "1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj",
            "2 0 obj<</Type/Pages/Count 1/Kids[3 0 R]>>endobj",
            "3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Contents 4 0 R/Resources<</Font<</F1 5 0 R>>>>>>endobj",
            $"4 0 obj<</Length {contentStream.Length}>>stream\n{contentStream}\nendstream endobj",
            "5 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj",
            "xref",
            "0 6",
            "0000000000 65535 f ",
            "0000000010 00000 n ",
            "0000000060 00000 n ",
            "0000000117 00000 n ",
            "0000000243 00000 n ",
            "0000000000 00000 n ",
            "trailer<</Size 6/Root 1 0 R>>",
            "startxref",
            "0",
            "%%EOF"
        };

        var fileName = $"job-{SanitizeFilePart(job.OrderNumber)}.pdf";
        return (Encoding.ASCII.GetBytes(string.Join("\n", lines)), fileName);
    }

    private static string GetPdfContentStream(
        JobDetailResponse job,
        bool nopicture,
        bool nocontent,
        Guid? supplierid,
        string? selectedpds)
    {
        static string Escape(string value) => value.Replace("(", "[").Replace(")", "]");

        var lines = new List<string>
        {
            "BT",
            "/F1 18 Tf",
            "50 740 Td",
            $"({Escape($"Job Order {job.OrderNumber}")}) Tj",
            "0 -24 Td",
            "/F1 12 Tf",
            $"({Escape($"Customer: {job.CustomerName}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Reference: {job.CustomerRef}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Title: {job.OrderTitle}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Ordered By: {job.OrderedBy}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Required On: {job.RequiredOn:yyyy-MM-dd}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Quantity: {job.Qty:0.##}")}) Tj",
            "0 -18 Td",
            $"({Escape($"No Picture: {nopicture}")}) Tj",
            "0 -18 Td",
            $"({Escape($"No Content: {nocontent}")}) Tj"
        };

        if (supplierid.HasValue)
        {
            lines.Add("0 -18 Td");
            lines.Add($"({Escape($"Supplier: {supplierid.Value}")}) Tj");
        }

        if (!string.IsNullOrWhiteSpace(selectedpds))
        {
            lines.Add("0 -18 Td");
            lines.Add($"({Escape($"Selected PDS: {selectedpds}")}) Tj");
        }

        if (!string.IsNullOrWhiteSpace(job.Remarks))
        {
            lines.Add("0 -18 Td");
            lines.Add($"({Escape($"Remarks: {job.Remarks}")}) Tj");
        }

        lines.Add("ET");
        return string.Join("\n", lines);
    }

    private static string SanitizeFilePart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "order";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var normalized = new string(value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());
        return normalized.Replace(' ', '-');
    }

    private static bool Contains(string value, string keyword)
    {
        return value.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private string? LocatePreviewFile(
        Guid orderId,
        string orderNumber,
        int jobNumber,
        DateTime? orderedOn,
        string fileName,
        string? attachmentType)
    {
        var probes = new List<string>();
        var normalizedAttachmentFolder = NormalizeFolderSegment(attachmentType);
        var orderedDate = orderedOn ?? DateTime.Now;
        var baseOrderNumber = ExtractBaseOrderNumber(orderNumber);
        var jobFolder = $"{baseOrderNumber}-{jobNumber}";
        var candidateFileNames = BuildPreviewFileNameCandidates(fileName);

        var fileAgentRoot = _configuration["LegacyFiles:FileAgentRoot"];
        foreach (var rootCandidate in ExpandRootCandidates(fileAgentRoot))
        {
            var legacyOrderFolder = Path.Combine(rootCandidate, baseOrderNumber);
            probes.Add(legacyOrderFolder);

            var migratedOrderFolder = Path.Combine(rootCandidate, "JB5", orderedDate.ToString("yyyy"), orderedDate.ToString("MM"), jobFolder);
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
                var directPath = FindDirectPath(folder, candidate);
                if (directPath is not null)
                {
                    return directPath;
                }

                var recursivePath = FindRecursivePathCaseInsensitive(folder, candidate);
                if (recursivePath is not null)
                {
                    return recursivePath;
                }
            }
        }

        return null;
    }

    private static string? FindDirectPath(string folder, string fileName)
    {
        if (!Directory.Exists(folder))
        {
            return null;
        }

        var path = Path.Combine(folder, fileName);
        return System.IO.File.Exists(path) ? path : null;
    }

    private static string? FindRecursivePathCaseInsensitive(string folder, string fileName)
    {
        if (!Directory.Exists(folder))
        {
            return null;
        }

        try
        {
            return Directory
                .EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                .FirstOrDefault(path => string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase));
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
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

    private async Task<(int Role, string Alias)> GetCurrentAccessAsync(CancellationToken cancellationToken)
    {
        var sid = ResolveCurrentSid();
        if (sid is null)
        {
            return (0, string.Empty);
        }

        var user = await _readContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserSid == sid.Value || x.UserId == sid.Value, cancellationToken);

        if (user is null)
        {
            return (0, string.Empty);
        }

        var userInfo = await _readContext.UserInfos
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == user.UserId || x.UserId == sid.Value, cancellationToken);

        return (userInfo?.UserRole ?? 0, user.Alias ?? string.Empty);
    }

    private Guid? ResolveCurrentSid()
    {
        var candidate = User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(candidate, out var sid) ? sid : null;
    }
}

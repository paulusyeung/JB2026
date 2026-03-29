using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using JB2026.Rest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Rest.Controllers;

[ApiController]
[Authorize]
[Route("api/CloudDisk")]
public sealed class CloudDiskCompatibilityController : ControllerBase
{
    private const int PageSize = 50;

    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;
    private readonly IConfiguration _configuration;

    public CloudDiskCompatibilityController(
        JB5LegacyReadContext readContext,
        JB5LegacyWriteContext writeContext,
        IConfiguration configuration)
    {
        _readContext = readContext;
        _writeContext = writeContext;
        _configuration = configuration;
    }

    [HttpGet("cups/{clientId:int}/{page:int}")]
    public IActionResult GetCups(int clientId, int page) => GetPagedFiles(clientId, page, "cups");

    [HttpGet("cups/keyword/{clientId:int}/{keyword}")]
    public IActionResult GetCupsByKeyword(int clientId, string keyword) => GetKeywordFiles(clientId, keyword, "cups");

    [HttpGet("cip3/{clientId:int}/{page:int}")]
    public IActionResult GetCip3(int clientId, int page) => GetPagedFiles(clientId, page, "cip3");

    [HttpGet("cip3/keyword/{clientId:int}/{keyword}")]
    public IActionResult GetCip3ByKeyword(int clientId, string keyword) => GetKeywordFiles(clientId, keyword, "cip3");

    [HttpGet("vps/{clientId:int}/{page:int}")]
    public IActionResult GetVps(int clientId, int page) => GetPagedFiles(clientId, page, "vps");

    [HttpGet("vps/keyword/{clientId:int}/{keyword}")]
    public IActionResult GetVpsByKeyword(int clientId, string keyword) => GetKeywordFiles(clientId, keyword, "vps");

    [HttpGet("Blueprint/{clientId:int}/{page:int}")]
    public IActionResult GetBlueprint(int clientId, int page) => GetPagedFiles(clientId, page, "Blueprint");

    [HttpGet("Blueprint/keyword/{clientId:int}/{keyword}")]
    public IActionResult GetBlueprintByKeyword(int clientId, string keyword) => GetKeywordFiles(clientId, keyword, "Blueprint");

    [HttpGet("plate/{clientId:int}/{page:int}")]
    public IActionResult GetPlate(int clientId, int page) => GetPagedFiles(clientId, page, "plate");

    [HttpGet("plate/keyword/{clientId:int}/{keyword}")]
    public IActionResult GetPlateByKeyword(int clientId, string keyword) => GetKeywordFiles(clientId, keyword, "plate");

    [HttpGet("film/{clientId:int}/{page:int}")]
    public IActionResult GetFilm(int clientId, int page) => GetPagedFiles(clientId, page, "film");

    [HttpGet("film/keyword/{clientId:int}/{keyword}")]
    public IActionResult GetFilmByKeyword(int clientId, string keyword) => GetKeywordFiles(clientId, keyword, "film");

    [HttpGet("thumbnail/{clientId:int}/{page:int}")]
    public IActionResult GetThumbnail(int clientId, int page) => GetPagedFiles(clientId, page, "thumbnail");

    [HttpGet("thumbnail/keyword/{clientId:int}/{keyword}")]
    public IActionResult GetThumbnailByKeyword(int clientId, string keyword) => GetKeywordFiles(clientId, keyword, "thumbnail");

    [HttpGet("tools/{clientId:int}/{page:int}")]
    public IActionResult GetTools(int clientId, int page) => GetPagedFiles(clientId, page, "tools");

    [HttpGet("tools/keyword/{clientId:int}/{keyword}")]
    public IActionResult GetToolsByKeyword(int clientId, string keyword) => GetKeywordFiles(clientId, keyword, "tools");

    [HttpGet("speedbox/{clientId:int}/{page:int}")]
    public IActionResult GetSpeedBox(int clientId, int page) => GetPagedFiles(clientId, page, "speedbox");

    [HttpGet("speedbox/keyword/{clientId:int}/{keyword}")]
    public IActionResult GetSpeedBoxByKeyword(int clientId, string keyword) => GetKeywordFiles(clientId, keyword, "speedbox");

    [HttpGet("users/subadmin/{workshop}")]
    public async Task<IActionResult> GetSubAdminUsers(string workshop, CancellationToken cancellationToken)
    {
        var users = await _readContext.Users
            .AsNoTracking()
            .Where(x => x.Status >= 1 && !x.Retired && x.Alias.Contains(workshop))
            .OrderBy(x => x.Alias)
            .Select(x => new { x.UserId, x.UserSid, x.Alias, x.LoginName })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpGet("thumbnail/{clientid:int}/{filename}/{width:int}/{height:int}")]
    public IActionResult GetThumbnailImage(int clientid, string filename, int width = 100, int height = 100)
    {
        _ = width;
        _ = height;

        var basePath = ResolveCloudDiskRoot();
        if (basePath is null)
        {
            return MissingRootResponse();
        }

        var target = Path.Combine(basePath, "thumbnail", clientid.ToString(), Path.GetFileName(filename));
        if (!System.IO.File.Exists(target))
        {
            return NotFound();
        }

        var content = System.IO.File.ReadAllBytes(target);
        return File(content, GetContentType(target));
    }

    [HttpPost("Action/Email/{id}")]
    public Task<IActionResult> PostActionEmail(
        string id,
        [FromBody] CloudDiskActionEmailRequest? request,
        CancellationToken cancellationToken)
        => PersistActionAsync("Email", id, request, cancellationToken);

    [HttpPost("Action/Reprint/{id}")]
    public Task<IActionResult> PostActionReprint(
        string id,
        [FromBody] CloudDiskActionReprintRequest? request,
        CancellationToken cancellationToken)
        => PersistActionAsync("Reprint", id, request, cancellationToken);

    [HttpPost("Action/Output/Blueprint/{id}")]
    public Task<IActionResult> PostActionOutputBlueprint(
        string id,
        [FromBody] CloudDiskActionOutputRequest? request,
        CancellationToken cancellationToken)
        => PersistActionAsync("OutputBlueprint", id, request, cancellationToken);

    [HttpPost("Action/Output/Plate/{id}")]
    public Task<IActionResult> PostActionOutputPlate(
        string id,
        [FromBody] CloudDiskActionOutputRequest? request,
        CancellationToken cancellationToken)
        => PersistActionAsync("OutputPlate", id, request, cancellationToken);

    [HttpPost("Action/Output/Film/{id}")]
    public Task<IActionResult> PostActionOutputFilm(
        string id,
        [FromBody] CloudDiskActionOutputRequest? request,
        CancellationToken cancellationToken)
        => PersistActionAsync("OutputFilm", id, request, cancellationToken);

    [HttpPost("fileAgent/upload/{id}")]
    public async Task<IActionResult> PostFileAgentUpload(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var orderId))
        {
            return BadRequest("Invalid order id.");
        }

        if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var basePath = ResolveCloudDiskRoot();
        if (basePath is null)
        {
            return MissingRootResponse();
        }

        var targetFolder = Path.Combine(basePath, "uploads", orderId.ToString("N"));
        Directory.CreateDirectory(targetFolder);

        foreach (var file in Request.Form.Files)
        {
            var fileName = Path.GetFileName(file.FileName);
            var targetPath = Path.Combine(targetFolder, fileName);

            await using var stream = System.IO.File.Create(targetPath);
            await file.CopyToAsync(stream, cancellationToken);

            _writeContext.JobAttachments.Add(new JobAttachment
            {
                AttachmentId = Guid.NewGuid(),
                OrderId = orderId,
                AttachmentType = 2,
                AttachmentIndex = 0,
                OriginalFileName = fileName
            });
        }

        await _writeContext.SaveChangesAsync(cancellationToken);
        return Ok("File uploaded.");
    }

    private IActionResult GetPagedFiles(int clientId, int page, string category)
    {
        var basePath = ResolveCloudDiskRoot();
        if (basePath is null)
        {
            return MissingRootResponse();
        }

        var folder = Path.Combine(basePath, category, clientId.ToString());
        if (!Directory.Exists(folder))
        {
            return Ok(Array.Empty<object>());
        }

        var normalizedPage = Math.Max(page, 1);
        var files = new DirectoryInfo(folder)
            .GetFiles("*", SearchOption.TopDirectoryOnly)
            .OrderByDescending(x => x.LastWriteTimeUtc)
            .Skip((normalizedPage - 1) * PageSize)
            .Take(PageSize)
            .Select(x => new
            {
                Name = x.Name,
                Size = x.Length,
                ModifiedOn = x.LastWriteTime,
                Category = category,
                ClientId = clientId
            })
            .ToList();

        return Ok(files);
    }

    private IActionResult GetKeywordFiles(int clientId, string keyword, string category)
    {
        var basePath = ResolveCloudDiskRoot();
        if (basePath is null)
        {
            return MissingRootResponse();
        }

        var folder = Path.Combine(basePath, category, clientId.ToString());
        if (!Directory.Exists(folder) || string.IsNullOrWhiteSpace(keyword))
        {
            return Ok(Array.Empty<object>());
        }

        var files = new DirectoryInfo(folder)
            .GetFiles("*", SearchOption.TopDirectoryOnly)
            .Where(x => x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.LastWriteTimeUtc)
            .Take(PageSize)
            .Select(x => new
            {
                Name = x.Name,
                Size = x.Length,
                ModifiedOn = x.LastWriteTime,
                Category = category,
                ClientId = clientId
            })
            .ToList();

        return Ok(files);
    }

    private string? ResolveCloudDiskRoot()
    {
        return _configuration["LegacyFiles:CloudDiskRoot"];
    }

    private async Task<IActionResult> PersistActionAsync(string action, string id, object? payload, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var actor = ResolveCurrentActor();
        var payloadSummary = BuildPayloadSummary(payload);

        _writeContext.FCMHistories.Add(new FCMHistory
        {
            FCMHistoryId = Guid.NewGuid(),
            MessageTitle = $"CloudDisk Action: {action}",
            MessageBody = $"ResourceId={id}; RequestedBy={actor}; Payload={payloadSummary}",
            DeliveredOn = now,
            Topic = "cloud-disk-action",
            RecipientList = action,
            UserIdList = actor
        });

        await _writeContext.SaveChangesAsync(cancellationToken);
        return Ok(new { Action = action, Id = id, Accepted = true, Recorded = true, QueuedAt = now });
    }

    private static string BuildPayloadSummary(object? payload)
    {
        if (payload is null)
        {
            return "null";
        }

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        return json.Length <= 400 ? json : json[..400];
    }

    private string ResolveCurrentActor()
    {
        var sid = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        return string.IsNullOrWhiteSpace(sid) ? "anonymous" : sid;
    }

    private static ObjectResult MissingRootResponse()
    {
        return new ObjectResult(new ProblemDetails
        {
            Title = "Not implemented",
            Detail = "Set configuration key 'LegacyFiles:CloudDiskRoot' to enable CloudDisk compatibility endpoints.",
            Status = StatusCodes.Status501NotImplemented
        })
        {
            StatusCode = StatusCodes.Status501NotImplemented
        };
    }

    private static string GetContentType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }
}

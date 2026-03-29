using System.Security.Claims;
using System.Text.Json;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Rest.Controllers;

[ApiController]
[Authorize]
[Route("api/fileAgent")]
public sealed class FileAgentCompatibilityController : ControllerBase
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;
    private readonly IConfiguration _configuration;

    public FileAgentCompatibilityController(
        JB5LegacyReadContext readContext,
        JB5LegacyWriteContext writeContext,
        IConfiguration configuration)
    {
        _readContext = readContext;
        _writeContext = writeContext;
        _configuration = configuration;
    }

    [HttpPost("jb5")]
    public async Task<IActionResult> PostFileAgentJb5(CancellationToken cancellationToken)
    {
        var fileAgentRoot = _configuration["LegacyFiles:FileAgentRoot"];
        if (string.IsNullOrWhiteSpace(fileAgentRoot))
        {
            return MissingLegacyPathResponse("LegacyFiles:FileAgentRoot");
        }

        var user = await ResolveCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var jobNumber = Request.Form["job-number"].ToString();
        if (!TryParseJobNumber(jobNumber, out var orderNumber, out var jobNo))
        {
            return BadRequest("Invalid job-number.");
        }

        var job = await _readContext.JobOrders
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrderNumber == orderNumber && x.JobNumber == jobNo, cancellationToken);

        if (job is null)
        {
            return NotFound();
        }

        var year = (job.OrderedOn ?? DateTime.Now).ToString("yyyy");
        var month = (job.OrderedOn ?? DateTime.Now).ToString("MM");
        var targetFolder = Path.Combine(fileAgentRoot, "JB5", year, month, jobNumber);
        Directory.CreateDirectory(targetFolder);

        await SaveUploadedFilesAsync(targetFolder, Request.Form.Files, cancellationToken);

        foreach (var file in Request.Form.Files)
        {
            _writeContext.JobAttachments.Add(new JobAttachment
            {
                AttachmentId = Guid.NewGuid(),
                OrderId = job.OrderId,
                AttachmentType = 2,
                AttachmentIndex = 0,
                OriginalFileName = Path.GetFileName(file.FileName)
            });
        }

        await _writeContext.SaveChangesAsync(cancellationToken);
        return Ok("File uploaded.");
    }

    [HttpPost("filing")]
    public async Task<IActionResult> PostFileAgentFiling(CancellationToken cancellationToken)
    {
        var fileAgentRoot = _configuration["LegacyFiles:FileAgentRoot"];
        if (string.IsNullOrWhiteSpace(fileAgentRoot))
        {
            return MissingLegacyPathResponse("LegacyFiles:FileAgentRoot");
        }

        var user = await ResolveCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var fileCategory = Request.Form["filing-category"].ToString();
        var fileFolder = Request.Form["file-folder"].ToString();
        var fileNumber = Request.Form["file-number"].ToString();

        if (string.IsNullOrWhiteSpace(fileCategory) || string.IsNullOrWhiteSpace(fileFolder) || string.IsNullOrWhiteSpace(fileNumber))
        {
            return BadRequest("Invalid filing form data.");
        }

        var safeCategory = fileCategory.Trim('\'', '/', '\\');
        var safeFolder = fileFolder.Trim('\'', '/', '\\');
        var safeNumber = fileNumber.Trim('\'', '/', '\\');

        var targetFolder = Path.Combine(fileAgentRoot, safeCategory, safeFolder, safeNumber);
        Directory.CreateDirectory(targetFolder);

        await SaveUploadedFilesAsync(targetFolder, Request.Form.Files, cancellationToken);
        return Ok("File uploaded.");
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> PostSubscribe(CancellationToken cancellationToken)
    {
        var user = await ResolveCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        string rawJson;
        using (var reader = new StreamReader(Request.Body))
        {
            rawJson = await reader.ReadToEndAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return BadRequest("Request body is required.");
        }

        using var jsonDoc = JsonDocument.Parse(rawJson);
        if (!TryParseSubscribePayload(jsonDoc.RootElement, out var deviceId, out var platform))
        {
            return BadRequest("Invalid subscribe payload.");
        }

        var existing = await _writeContext.UserNotifications
            .SingleOrDefaultAsync(x => x.UserId == user.UserId && x.DeviceId == deviceId && x.NotifyType == 30, cancellationToken);

        if (existing is null)
        {
            _writeContext.UserNotifications.Add(new UserNotification
            {
                NotifyId = Guid.NewGuid(),
                UserId = user.UserId,
                DeviceId = deviceId,
                NotifyType = 30,
                Platform = platform,
                MetadataXml = rawJson
            });
        }
        else
        {
            existing.Platform = platform;
            existing.MetadataXml = rawJson;
        }

        await _writeContext.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    [HttpGet("filingCategory")]
    public IActionResult GetFilingCategory()
    {
        var categories = new[] { "CUPS", "CIP3", "VPS", "Blueprint", "Plate", "Film", "Thumbnail", "Tools", "SpeedBox" };
        return Ok(categories);
    }

    private async Task SaveUploadedFilesAsync(string targetFolder, IFormFileCollection files, CancellationToken cancellationToken)
    {
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file.FileName);
            var targetPath = Path.Combine(targetFolder, fileName);

            await using var stream = System.IO.File.Create(targetPath);
            await file.CopyToAsync(stream, cancellationToken);
        }
    }

    private async Task<User?> ResolveCurrentUserAsync(CancellationToken cancellationToken)
    {
        var sid = ResolveCurrentSid();
        if (sid is null)
        {
            return null;
        }

        return await _readContext.Users
            .SingleOrDefaultAsync(x => x.UserSid == sid.Value || x.UserId == sid.Value, cancellationToken);
    }

    private Guid? ResolveCurrentSid()
    {
        var candidate = User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(candidate, out var sid) ? sid : null;
    }

    private static bool TryParseJobNumber(string jobNumber, out string orderNumber, out int jobNo)
    {
        orderNumber = string.Empty;
        jobNo = 0;

        if (string.IsNullOrWhiteSpace(jobNumber))
        {
            return false;
        }

        var parts = jobNumber.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || parts[0].Length < 6 || !int.TryParse(parts[1], out jobNo))
        {
            return false;
        }

        orderNumber = parts[0];
        return true;
    }

    private static bool TryParseSubscribePayload(JsonElement root, out string deviceId, out int platform)
    {
        deviceId = string.Empty;
        platform = 0;

        if (!TryGetPropertyCaseInsensitive(root, "DeviceInfo", out var deviceInfo)
            || !TryGetPropertyCaseInsensitive(deviceInfo, "Id", out var deviceIdProp)
            || deviceIdProp.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        deviceId = deviceIdProp.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return false;
        }

        if (TryGetPropertyCaseInsensitive(deviceInfo, "Platform", out var platformProp) && platformProp.TryGetInt32(out var parsedPlatform))
        {
            platform = parsedPlatform;
        }

        return true;
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement source, string propertyName, out JsonElement value)
    {
        if (source.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in source.EnumerateObject())
            {
                if (property.NameEquals(propertyName) || property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static ObjectResult MissingLegacyPathResponse(string key)
    {
        return new ObjectResult(new ProblemDetails
        {
            Title = "Not implemented",
            Detail = $"Set configuration key '{key}' to enable this endpoint.",
            Status = StatusCodes.Status501NotImplemented
        })
        {
            StatusCode = StatusCodes.Status501NotImplemented
        };
    }
}

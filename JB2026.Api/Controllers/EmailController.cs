using System.Security.Claims;
using System.Xml.Linq;
using JB2026.Api.Models;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/email")]
public sealed class EmailController : ControllerBase
{
    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyList<EmailMessageResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EmailMessageResponse>>> SearchEmails(
        [FromQuery] string? lookup,
        [FromServices] IEmailService emailService,
        [FromServices] JB5LegacyReadContext readContext,
        CancellationToken cancellationToken = default)
    {
        var currentUserEmail = await ResolveCurrentUserEmailAsync(readContext, cancellationToken);

        var domain = lookup?.Trim() ?? string.Empty;

        var results = await emailService.GetEmailsAsync(
            domain,
            [],
            currentUserEmail,
            null,
            null,
            cancellationToken);

        return Ok(results);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EmailDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmailDetailResponse>> GetEmailDetail(
        string id,
        [FromQuery] string folder,
        [FromServices] IEmailService emailService,
        CancellationToken cancellationToken = default)
    {
        var detail = await emailService.GetEmailDetailAsync(id, folder, cancellationToken);
        if (detail is null)
            return NotFound();

        return Ok(detail);
    }

    [HttpGet("{id}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadAttachment(
        string id,
        [FromQuery] string folder,
        [FromQuery] string fileName,
        [FromServices] IEmailService emailService,
        CancellationToken cancellationToken = default)
    {
        var result = await emailService.DownloadAttachmentAsync(id, folder, fileName, cancellationToken);
        if (result is null)
            return NotFound();

        return File(result.Content, result.ContentType, result.FileName);
    }

    private async Task<string?> ResolveCurrentUserEmailAsync(JB5LegacyReadContext readContext, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;

        var userInfo = await readContext.UserInfos
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (userInfo?.MetadataXml is null)
            return null;

        return ExtractEmailFromMetadata(userInfo.MetadataXml);
    }

    private static string ExtractEmailFromMetadata(string? metadataXml)
    {
        if (string.IsNullOrWhiteSpace(metadataXml))
            return string.Empty;

        try
        {
            var xml = XElement.Parse(metadataXml);
            return xml.Element("Email")?.Value?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}

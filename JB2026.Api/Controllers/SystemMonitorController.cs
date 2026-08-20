using JB2026.Api.Models;
using JB2026.Api.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JB2026.Api.Controllers;

/// <summary>
/// Exposes read-only integration settings (CRM, DMS, Email) for the system monitor screen.
/// Secret values are masked before being returned to the client.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v2/system-monitor")]
public sealed class SystemMonitorController : ControllerBase
{
    private readonly IOptions<TwentyCrmOptions> _crmOptions;
    private readonly IOptions<PaperlessNgxOptions> _dmsOptions;
    private readonly IOptions<MailcowOptions> _emailOptions;

    public SystemMonitorController(
        IOptions<TwentyCrmOptions> crmOptions,
        IOptions<PaperlessNgxOptions> dmsOptions,
        IOptions<MailcowOptions> emailOptions)
    {
        _crmOptions = crmOptions;
        _dmsOptions = dmsOptions;
        _emailOptions = emailOptions;
    }

    /// <summary>
    /// Returns the CRM, DMS, and Email integration settings from appsettings.json.
    /// </summary>
    /// <response code="200">Settings overview returned successfully.</response>
    /// <response code="401">Unauthorized.</response>
    [HttpGet("settings")]
    [ProducesResponseType(typeof(SystemMonitorSettingsResponse), StatusCodes.Status200OK)]
    public ActionResult<SystemMonitorSettingsResponse> GetSettings()
    {
        var crm = _crmOptions.Value;
        var dms = _dmsOptions.Value;
        var email = _emailOptions.Value;

        return Ok(new SystemMonitorSettingsResponse
        {
            Crm = new CrmSettingsResponse
            {
                Configured = !string.IsNullOrWhiteSpace(crm.ApiKey) && !string.IsNullOrWhiteSpace(crm.BaseUrl),
                BaseUrl = crm.BaseUrl,
                ApiKey = MaskSecret(crm.ApiKey),
                HttpClientTimeoutSeconds = crm.HttpClientTimeoutSeconds,
            },
            Dms = new DmsSettingsResponse
            {
                Configured = !string.IsNullOrWhiteSpace(dms.BaseUrl)
                    && !string.IsNullOrWhiteSpace(dms.ApiToken)
                    && !string.IsNullOrWhiteSpace(dms.DefaultUser),
                BaseUrl = dms.BaseUrl,
                ApiToken = MaskSecret(dms.ApiToken),
                DefaultUser = dms.DefaultUser,
                HttpClientTimeoutSeconds = dms.HttpClientTimeoutSeconds,
            },
            Email = new EmailSettingsResponse
            {
                Configured = !string.IsNullOrWhiteSpace(email.BaseUrl)
                    && !string.IsNullOrWhiteSpace(email.FallbackAccountEmail),
                BaseUrl = email.BaseUrl,
                FallbackAccountEmail = email.FallbackAccountEmail,
                FallbackAccountPassword = MaskSecret(email.FallbackAccountPassword),
                ImapPort = email.ImapPort,
                UseSsl = email.UseSsl,
                HttpClientTimeoutSeconds = email.HttpClientTimeoutSeconds,
            },
        });
    }

    private static string MaskSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length <= 8
            ? "********"
            : value[..4] + "****" + value[^4..];
    }
}
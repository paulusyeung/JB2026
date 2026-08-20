namespace JB2026.Api.Models;

/// <summary>
/// Settings overview for the system monitor dashboard.
/// Sensitive values (API keys, tokens, passwords) are masked before being returned.
/// </summary>
public sealed class SystemMonitorSettingsResponse
{
    public required CrmSettingsResponse Crm { get; init; }

    public required DmsSettingsResponse Dms { get; init; }

    public required EmailSettingsResponse Email { get; init; }
}

/// <summary>
/// CRM (Twenty) integration settings from appsettings.json.
/// </summary>
public sealed class CrmSettingsResponse
{
    public required bool Configured { get; init; }

    public required string BaseUrl { get; init; }

    public required string ApiKey { get; init; }

    public required int HttpClientTimeoutSeconds { get; init; }
}

/// <summary>
/// DMS (Paperless-ngx) integration settings from appsettings.json.
/// </summary>
public sealed class DmsSettingsResponse
{
    public required bool Configured { get; init; }

    public required string BaseUrl { get; init; }

    public required string ApiToken { get; init; }

    public required string DefaultUser { get; init; }

    public required int HttpClientTimeoutSeconds { get; init; }
}

/// <summary>
/// Email (Mailcow) integration settings from appsettings.json.
/// </summary>
public sealed class EmailSettingsResponse
{
    public required bool Configured { get; init; }

    public required string BaseUrl { get; init; }

    public required string FallbackAccountEmail { get; init; }

    public required string FallbackAccountPassword { get; init; }

    public required int ImapPort { get; init; }

    public required bool UseSsl { get; init; }

    public required int HttpClientTimeoutSeconds { get; init; }
}
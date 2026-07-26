namespace JB2026.Api.Options;

public class MailcowOptions
{
    public const string SectionName = "Mailcow";

    public string BaseUrl { get; set; } = string.Empty;

    public string FallbackAccountEmail { get; set; } = string.Empty;

    public string FallbackAccountPassword { get; set; } = string.Empty;

    public int ImapPort { get; set; } = 993;

    public bool UseSsl { get; set; } = true;

    public int HttpClientTimeoutSeconds { get; set; } = 15;
}

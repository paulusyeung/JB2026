namespace JB2026.Api.Options;

public class PaperlessNgxOptions
{
    public const string SectionName = "PaperlessNgx";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiToken { get; set; } = string.Empty;

    public int HttpClientTimeoutSeconds { get; set; } = 30;
}

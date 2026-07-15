namespace JB2026.Api.Options;

public class TwentyCrmOptions
{
    public const string SectionName = "TwentyCrm";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public int HttpClientTimeoutSeconds { get; set; } = 30;
}

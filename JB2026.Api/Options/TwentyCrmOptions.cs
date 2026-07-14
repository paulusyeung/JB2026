namespace JB2026.Api.Options;

public class TwentyCrmOptions
{
    public const string SectionName = "TwentyCrm";

    public string ConnectionString { get; set; } = string.Empty;

    public string WorkspaceId { get; set; } = string.Empty;
}

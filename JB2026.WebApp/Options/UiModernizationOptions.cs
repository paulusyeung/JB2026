namespace JB2026.WebApp.Options;

public sealed class UiModernizationOptions
{
    public const string SectionName = "UiModernization";

    public int CacheTtlSeconds { get; set; } = 60;

    public string LegacyBaseUrl { get; set; } = string.Empty;

    public Dictionary<string, UiSliceFlagOptions> Slices { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class UiSliceFlagOptions
{
    public string DisplayName { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public string[] Prefixes { get; set; } = [];
}
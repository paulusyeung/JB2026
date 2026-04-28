namespace JB2026.Api.Options;

public sealed class LegacyFilesOptions
{
    public const string SectionName = "LegacyFiles";

    public string DropBox { get; set; } = string.Empty;
    public string InBox { get; set; } = string.Empty;
    public string OutBox { get; set; } = string.Empty;
    public string WorkFolder { get; set; } = string.Empty;
    public string FileAgentRoot { get; set; } = string.Empty;
    public string CloudDiskRoot { get; set; } = string.Empty;
    public string ProductPictureRoot { get; set; } = string.Empty;
    public string SmlFileRoot { get; set; } = string.Empty;
}

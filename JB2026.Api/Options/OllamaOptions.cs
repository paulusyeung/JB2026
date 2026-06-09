namespace JB2026.Api.Options;

public class OllamaOptions
{
    public const string SectionName = "Ollama";

    public bool Enabled { get; set; } = true;

    public string BaseUrl { get; set; } = "http://localhost:11434";

    public string DefaultModel { get; set; } = "llama3";

    public int TimeoutSeconds { get; set; } = 30;
}

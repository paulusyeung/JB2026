namespace JB2026.Infrastructure.Options;

public sealed class Jb2026ObservabilityOptions
{
    public const string SectionName = "JB2026:Observability";

    public string ServiceName { get; init; } = "JB2026";
    public string? OtlpEndpoint { get; init; }
}
namespace JB2026.Infrastructure.Options;

public sealed class Jb2026EnvironmentOptions
{
    public const string SectionName = "JB2026:Environment";

    public string DeploymentRing { get; init; } = "Development";
    public string SecretProvider { get; init; } = "EnvironmentVariables";
}
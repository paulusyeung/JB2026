namespace JB2026.Api.Options;

public sealed class JobListOptions
{
    public const string SectionName = "JobList";

    public int InitialTake { get; init; } = 300;

    public int FilteredTake { get; init; } = 2000;

    public int MaxTake { get; init; } = 5000;
}
namespace JB2026.Api.Options;

public sealed class JobListOptions
{
    public const string SectionName = "JobList";

    public int InitialTake { get; init; } = 300;

    public int FilteredTake { get; init; } = 2000;

    public int MaxTake { get; init; } = 5000;

    /// <summary>Default number of push-history entries returned by the job timeline endpoint.</summary>
    public int TimelineTake { get; init; } = 200;

    /// <summary>Upper bound for a single job timeline read so one long-lived order cannot return the whole table.</summary>
    public int MaxTimelineTake { get; init; } = 500;
}
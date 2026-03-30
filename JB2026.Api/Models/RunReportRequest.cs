namespace JB2026.Api.Models;

public sealed class RunReportRequest
{
    public string ReportName { get; init; } = "Exceptional_Report";

    public DateOnly StartOn { get; init; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public int Days { get; init; } = 31;

    public int Take { get; init; } = 100;
}
namespace JB2026.Rest.Models;

public sealed class VwJobScheduleEx
{
    public Guid? OrderId { get; set; }
    public int? OrderType { get; set; }
    public string? OrderNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? OrderTitle { get; set; }
    public int? ScheduleCount { get; set; }
    public int? Priority { get; set; }
    public string? MachineNumber { get; set; }
    public int? Status { get; set; }
    public DateTime? ScheduledOn { get; set; }
    public DateTime? CompletedOn { get; set; }
    public DateTime? OrderedOn { get; set; }
    public DateTime? RequiredOn { get; set; }
    public bool ShouldReview { get; set; }
    public Guid ScheduleId { get; set; }
    public int UrgencyLevel { get; set; }
    public string? OrderedBy { get; set; }
    public string? OutputRef { get; set; }
    public string? PrintInfo_1 { get; set; }
    public string? PrintInfo_2 { get; set; }
    public string? PrintInfo_3 { get; set; }
    public int Light_1 { get; set; }
    public int Light_2 { get; set; }
}
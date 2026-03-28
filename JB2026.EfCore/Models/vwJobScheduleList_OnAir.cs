using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwJobScheduleList_OnAir
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

    public int UrgencyLevel { get; set; }

    public bool ShouldReview { get; set; }

    public string? OutputRef { get; set; }
}

using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwJobSchedule_OnAirList
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

    public bool ShouldReview { get; set; }

    public Guid ScheduleId { get; set; }

    public int UrgencyLevel { get; set; }

    public string? OrderedBy { get; set; }
}

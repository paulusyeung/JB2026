using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class JobSchedule
{
    public Guid ScheduleId { get; set; }

    public Guid OrderId { get; set; }

    public DateTime? ScheduledOn { get; set; }

    public int? Status { get; set; }

    public int? Priority { get; set; }

    public string? MachineNumber { get; set; }

    public DateTime? CompletedOn { get; set; }

    public bool? ShouldReview { get; set; }

    public int UrgencyLevel { get; set; }

    public bool? Cancelled { get; set; }

    public DateTime? CancelledOn { get; set; }

    public Guid? CancelledBy { get; set; }

    public int? RescheduledCount { get; set; }

    public Guid? RescheduledBy { get; set; }

    public DateTime? RescheduledOn { get; set; }

    public virtual JobOrder Order { get; set; } = null!;
}

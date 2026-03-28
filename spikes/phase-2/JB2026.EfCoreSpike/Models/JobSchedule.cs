using System;
using System.Collections.Generic;

namespace JB2026.EfCoreSpike.Models;

public partial class JobSchedule
{
    public Guid JobScheduleId { get; set; }

    public Guid OrderId { get; set; }

    public string? MachineNumber { get; set; }

    public DateTime? ScheduledOn { get; set; }

    public DateTime? CompletedOn { get; set; }

    public int Status { get; set; }

    public int Priority { get; set; }

    public virtual JobOrder Order { get; set; } = null!;
}

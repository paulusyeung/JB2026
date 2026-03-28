using System;
using System.Collections.Generic;

namespace JB2026.EfCoreSpike.Models;

public partial class JobWorkflow
{
    public Guid JobWorkflowId { get; set; }

    public Guid OrderId { get; set; }

    public int WorkStatus { get; set; }

    public int WorkIndex { get; set; }

    public string? WorkNotes { get; set; }

    public virtual JobOrder Order { get; set; } = null!;
}

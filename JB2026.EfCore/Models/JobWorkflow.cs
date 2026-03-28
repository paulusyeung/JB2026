using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class JobWorkflow
{
    public Guid JobWorkflowId { get; set; }

    public Guid OrderId { get; set; }

    public Guid? WorkflowId { get; set; }

    public int WorkIndex { get; set; }

    public string? WorkTitle { get; set; }

    public string? WorkInstruction { get; set; }

    public int? WorkStatus { get; set; }

    public string? WorkNotes { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public Guid? ModifiedBy { get; set; }

    public virtual ICollection<JobWorkflowForm> JobWorkflowForms { get; set; } = new List<JobWorkflowForm>();

    public virtual JobOrder Order { get; set; } = null!;

    public virtual Z_Workflow? Workflow { get; set; }
}

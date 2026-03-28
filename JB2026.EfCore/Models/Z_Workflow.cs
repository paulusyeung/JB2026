using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class Z_Workflow
{
    public Guid WorkflowId { get; set; }

    public string? WorkflowName { get; set; }

    public string? WorkTitle { get; set; }

    public string? WorkInstruction { get; set; }

    public virtual ICollection<JobWorkflow> JobWorkflows { get; set; } = new List<JobWorkflow>();

    public virtual ICollection<Z_OrderTypeWorkflow> Z_OrderTypeWorkflows { get; set; } = new List<Z_OrderTypeWorkflow>();

    public virtual ICollection<Z_WorkflowForm> Z_WorkflowForms { get; set; } = new List<Z_WorkflowForm>();
}

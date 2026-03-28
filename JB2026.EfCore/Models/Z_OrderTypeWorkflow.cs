using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class Z_OrderTypeWorkflow
{
    public Guid OrderTypeWorkflowId { get; set; }

    public Guid? WorkflowId { get; set; }

    public int OrderType { get; set; }

    public int WorkIndex { get; set; }

    public virtual Z_Workflow? Workflow { get; set; }
}

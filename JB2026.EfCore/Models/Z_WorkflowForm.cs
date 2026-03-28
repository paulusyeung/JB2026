using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class Z_WorkflowForm
{
    public Guid WorkflowFormId { get; set; }

    public Guid? WorkflowId { get; set; }

    public Guid? FormId { get; set; }

    public int SeqNumber { get; set; }

    public virtual Z_Form? Form { get; set; }

    public virtual Z_Workflow? Workflow { get; set; }
}

using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class Z_Form
{
    public Guid FormId { get; set; }

    public int FormObjectEnum { get; set; }

    public string? FormName { get; set; }

    public string? FormName_Chs { get; set; }

    public string? FormName_Cht { get; set; }

    public string? MetadataXml { get; set; }

    public virtual ICollection<JobWorkflowForm> JobWorkflowForms { get; set; } = new List<JobWorkflowForm>();

    public virtual ICollection<Z_WorkflowForm> Z_WorkflowForms { get; set; } = new List<Z_WorkflowForm>();
}

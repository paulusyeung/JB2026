using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class JobWorkflowForm
{
    public Guid JobWorkflowFormId { get; set; }

    public Guid JobWorkflowId { get; set; }

    public Guid? FormId { get; set; }

    public int? SeqNumber { get; set; }

    public string? MetadataXml { get; set; }

    public virtual Z_Form? Form { get; set; }

    public virtual JobWorkflow JobWorkflow { get; set; } = null!;
}

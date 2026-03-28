using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class JobAttachment
{
    public Guid AttachmentId { get; set; }

    public Guid OrderId { get; set; }

    public int AttachmentType { get; set; }

    public int AttachmentIndex { get; set; }

    public string? OriginalFileName { get; set; }

    public virtual JobOrder Order { get; set; } = null!;
}

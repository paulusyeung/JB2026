using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class ProductAttachment
{
    public Guid AttachmentId { get; set; }

    public Guid ProductId { get; set; }

    public int AttachmentIndex { get; set; }

    public string? OriginalFileName { get; set; }

    public virtual Product Product { get; set; } = null!;
}

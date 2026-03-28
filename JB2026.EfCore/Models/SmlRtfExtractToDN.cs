using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class SmlRtfExtractToDN
{
    public Guid DNId { get; set; }

    public Guid HeaderId { get; set; }

    public string? DNNumber { get; set; }

    public DateTime DNDate { get; set; }

    public int? DNType { get; set; }

    public DateTime CreatedOn { get; set; }

    public Guid CreatedBy { get; set; }

    public virtual SmlRtfHeader Header { get; set; } = null!;
}

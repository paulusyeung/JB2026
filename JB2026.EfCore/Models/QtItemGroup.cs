using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class QtItemGroup
{
    public Guid ItemGroupId { get; set; }

    public string Zone { get; set; } = null!;

    public string? GroupNameEn { get; set; }

    public string? GroupNameCht { get; set; }

    public string? GroupNameChs { get; set; }

    public DateTime CreatedOn { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public Guid ModifiedBy { get; set; }

    public bool Retired { get; set; }

    public DateTime? RetiredOn { get; set; }

    public Guid? RetiredBy { get; set; }

    public virtual ICollection<QtItem> QtItems { get; set; } = new List<QtItem>();
}

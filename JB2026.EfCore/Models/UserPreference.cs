using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class UserPreference
{
    public Guid PreferenceId { get; set; }

    public Guid UserId { get; set; }

    public Guid ObjectId { get; set; }

    public int ObjectType { get; set; }

    public string? MetadataXml { get; set; }

    public virtual User User { get; set; } = null!;
}

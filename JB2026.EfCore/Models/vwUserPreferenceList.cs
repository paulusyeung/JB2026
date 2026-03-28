using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwUserPreferenceList
{
    public Guid UserId { get; set; }

    public int UserType { get; set; }

    public Guid UserSid { get; set; }

    public string LoginName { get; set; } = null!;

    public string LoginPassword { get; set; } = null!;

    public string Alias { get; set; } = null!;

    public int Status { get; set; }

    public bool Retired { get; set; }

    public Guid PreferenceId { get; set; }

    public Guid ObjectId { get; set; }

    public int ObjectType { get; set; }

    public string? MetadataXml { get; set; }
}

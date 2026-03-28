using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class UserAuth
{
    public Guid AuthId { get; set; }

    public Guid UserId { get; set; }

    public string DeviceId { get; set; } = null!;

    public int AuthType { get; set; }

    public int Platform { get; set; }

    public string? MetadataXml { get; set; }

    public virtual User User { get; set; } = null!;
}

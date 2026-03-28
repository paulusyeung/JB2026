using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class UserNotification
{
    public Guid NotifyId { get; set; }

    public Guid UserId { get; set; }

    public string DeviceId { get; set; } = null!;

    public int NotifyType { get; set; }

    public int Platform { get; set; }

    public string? MetadataXml { get; set; }

    public virtual User User { get; set; } = null!;
}

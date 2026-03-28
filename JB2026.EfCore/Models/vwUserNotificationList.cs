using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwUserNotificationList
{
    public Guid NotifyId { get; set; }

    public string DeviceId { get; set; } = null!;

    public int NotifyType { get; set; }

    public int Platform { get; set; }

    public string? NotifyXml { get; set; }

    public int AuthType { get; set; }

    public string? AuthXml { get; set; }

    public Guid UserId { get; set; }

    public int UserType { get; set; }

    public Guid UserSid { get; set; }

    public string LoginName { get; set; } = null!;

    public string LoginPassword { get; set; } = null!;

    public string Alias { get; set; } = null!;

    public int Status { get; set; }

    public int UserRole { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public bool Retired { get; set; }

    public DateTime? RetiredOn { get; set; }

    public string? RetiredBy { get; set; }
}

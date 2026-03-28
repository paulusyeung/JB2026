using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class User
{
    public Guid UserId { get; set; }

    public int UserType { get; set; }

    public Guid UserSid { get; set; }

    public string LoginName { get; set; } = null!;

    public string LoginPassword { get; set; } = null!;

    public string Alias { get; set; } = null!;

    public int Status { get; set; }

    public DateTime CreatedOn { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public Guid ModifiedBy { get; set; }

    public bool Retired { get; set; }

    public DateTime? RetiredOn { get; set; }

    public Guid? RetiredBy { get; set; }

    public virtual ICollection<UserAuth> UserAuths { get; set; } = new List<UserAuth>();

    public virtual ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();

    public virtual ICollection<UserPreference> UserPreferences { get; set; } = new List<UserPreference>();
}

using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class UserInfo
{
    public Guid UserId { get; set; }

    public bool PrimaryRec { get; set; }

    public string? UserName { get; set; }

    public string? UserPassword { get; set; }

    public string? UserAlias { get; set; }

    public int UserRole { get; set; }

    public string? MetadataXml { get; set; }

    public DateTime CreatedOn { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public Guid ModifiedBy { get; set; }

    public bool Retired { get; set; }

    public DateTime? RetiredOn { get; set; }

    public Guid? RetiredBy { get; set; }
}

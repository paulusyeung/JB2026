using System;
using System.Collections.Generic;

namespace JB2026.EfCore.Models;

public partial class vwUserList_Active
{
    public Guid UserId { get; set; }

    public bool PrimaryRec { get; set; }

    public string? UserName { get; set; }

    public string? UserPassword { get; set; }

    public string? UserAlias { get; set; }

    public int UserRole { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string? ModifiedBy { get; set; }
}

namespace JB2026.Api.Models;

public sealed class AdminUserRecordResponse
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string UserAlias { get; init; } = string.Empty;
    public string UserPassword { get; init; } = string.Empty;
    public int UserRole { get; init; }
    public string Role { get; init; } = string.Empty;
    public bool PrimaryRec { get; init; }
    public DateTime CreatedOn { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime ModifiedOn { get; init; }
    public string ModifiedBy { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

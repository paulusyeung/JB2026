namespace JB2026.Api.Models;

public sealed class AdminUserResponse
{
    public required Guid UserId { get; init; }

    public required string Username { get; init; }

    public required string DisplayName { get; init; }

    public required string Role { get; init; }

    public required bool PrimaryRec { get; init; }

    public required string UserAlias { get; init; }

    public required string UserPassword { get; init; }

    public required DateTime CreatedOn { get; init; }

    public required string CreatedBy { get; init; }

    public required DateTime ModifiedOn { get; init; }

    public required string ModifiedBy { get; init; }
}
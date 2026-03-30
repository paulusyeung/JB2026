namespace JB2026.Api.Models;

public sealed class AdminUserResponse
{
    public required Guid UserId { get; init; }

    public required string Username { get; init; }

    public required string DisplayName { get; init; }

    public required string Role { get; init; }
}
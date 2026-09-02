using System.ComponentModel.DataAnnotations;

namespace JB2026.Api.Models;

public sealed class CreateAdminUserRequest
{
    [Required]
    [StringLength(64)]
    public string Username { get; init; } = string.Empty;

    [StringLength(64)]
    public string UserAlias { get; init; } = string.Empty;

    [StringLength(64)]
    public string UserPassword { get; init; } = string.Empty;

    [Range(0, 4)]
    public int UserRole { get; init; } = 0;

    [StringLength(254)]
    public string Email { get; init; } = string.Empty;
}

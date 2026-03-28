namespace JB2026.ApiPilot.Models;

public sealed class TokenRequest
{
    public Guid UserId { get; init; } = Guid.Parse("f31c57ea-7f08-4a05-b5b5-58b2cdab1001");

    public string DisplayName { get; init; } = "Phase 2 Spike User";

    public string Role { get; init; } = "Manager";
}
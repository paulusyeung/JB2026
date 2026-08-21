namespace JB2026.Api.Services;

public interface IRbacService
{
    Task<RbacSnapshot> GetGroupRbacAsync(string role, CancellationToken cancellationToken = default);

    Task<RbacSnapshot> GetEffectiveRbacAsync(CancellationToken cancellationToken = default);

    Task SaveGroupRbacAsync(string role, IReadOnlyDictionary<string, bool> values, CancellationToken cancellationToken = default);

    Task<RbacSnapshot> GetUserRbacAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SaveUserRbacAsync(Guid userId, IReadOnlyDictionary<string, bool> values, CancellationToken cancellationToken = default);
}

public sealed record RbacSnapshot(
    string? TargetName,
    IReadOnlyDictionary<string, bool> Values);

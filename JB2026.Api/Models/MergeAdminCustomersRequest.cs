namespace JB2026.Api.Models;

public sealed class MergeAdminCustomersRequest
{
    public Guid TargetCustomerId { get; init; }

    public IReadOnlyList<Guid> CustomerIds { get; init; } = [];
}
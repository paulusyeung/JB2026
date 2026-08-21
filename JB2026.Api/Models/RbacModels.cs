namespace JB2026.Api.Models;

public sealed class RbacValuesResponse
{
    public IReadOnlyDictionary<string, bool> Values { get; init; } = new Dictionary<string, bool>();
}

public sealed class SaveRbacRequest
{
    public IReadOnlyDictionary<string, bool> Values { get; init; } = new Dictionary<string, bool>();
}

namespace JB2026.WebApp.Services;

public interface IUiFeatureFlagStore
{
    IReadOnlyList<UiSliceFlagSnapshot> GetCurrentSlices();

    ValueTask<UiSliceRouteDecision?> ResolveAsync(PathString requestPath, CancellationToken cancellationToken = default);
}

public sealed record UiSliceFlagSnapshot(
    string Key,
    string DisplayName,
    bool Enabled,
    IReadOnlyList<string> Prefixes);

public sealed record UiSliceRouteDecision(
    string Key,
    string DisplayName,
    bool Enabled,
    string LegacyBaseUrl);
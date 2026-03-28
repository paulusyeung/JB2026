using JB2026.WebApp.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace JB2026.WebApp.Services;

public sealed class ConfigurationUiFeatureFlagStore : IUiFeatureFlagStore
{
    private const string CacheKeyPrefix = "ui-slice-enabled:";
    private readonly IMemoryCache _memoryCache;
    private readonly IOptionsMonitor<UiModernizationOptions> _optionsMonitor;

    public ConfigurationUiFeatureFlagStore(IMemoryCache memoryCache, IOptionsMonitor<UiModernizationOptions> optionsMonitor)
    {
        _memoryCache = memoryCache;
        _optionsMonitor = optionsMonitor;
    }

    public IReadOnlyList<UiSliceFlagSnapshot> GetCurrentSlices()
    {
        var options = _optionsMonitor.CurrentValue;
        return options.Slices
            .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Select(entry => new UiSliceFlagSnapshot(
                entry.Key,
                string.IsNullOrWhiteSpace(entry.Value.DisplayName) ? entry.Key : entry.Value.DisplayName,
                entry.Value.Enabled,
                entry.Value.Prefixes.Where(prefix => !string.IsNullOrWhiteSpace(prefix)).ToArray()))
            .ToArray();
    }

    public ValueTask<UiSliceRouteDecision?> ResolveAsync(PathString requestPath, CancellationToken cancellationToken = default)
    {
        var options = _optionsMonitor.CurrentValue;
        var pathValue = requestPath.Value ?? "/";

        foreach (var snapshot in GetOrderedSnapshots(options))
        {
            if (!snapshot.Prefixes.Any(prefix => MatchesPrefix(pathValue, prefix)))
            {
                continue;
            }

            var enabled = _memoryCache.GetOrCreate(CacheKeyPrefix + snapshot.Key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(options.CacheTtlSeconds <= 0 ? 60 : options.CacheTtlSeconds);
                return snapshot.Enabled;
            });

            return ValueTask.FromResult<UiSliceRouteDecision?>(new UiSliceRouteDecision(
                snapshot.Key,
                snapshot.DisplayName,
                enabled,
                options.LegacyBaseUrl));
        }

        return ValueTask.FromResult<UiSliceRouteDecision?>(null);
    }

    private static IReadOnlyList<UiSliceFlagSnapshot> GetOrderedSnapshots(UiModernizationOptions options)
    {
        return options.Slices
            .Select(entry => new UiSliceFlagSnapshot(
                entry.Key,
                string.IsNullOrWhiteSpace(entry.Value.DisplayName) ? entry.Key : entry.Value.DisplayName,
                entry.Value.Enabled,
                entry.Value.Prefixes.Where(prefix => !string.IsNullOrWhiteSpace(prefix)).ToArray()))
            .OrderByDescending(snapshot => snapshot.Prefixes.Max(prefix => prefix.Length))
            .ToArray();
    }

    private static bool MatchesPrefix(string requestPath, string prefix)
    {
        if (string.Equals(requestPath, prefix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return requestPath.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
    }
}
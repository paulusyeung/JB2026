namespace JB2026.WebApp.Models;

public sealed record LegacySliceSampleRouteResponse(
    string Path,
    string Description);

public sealed record LegacySliceViewModelResponse(
    string Key,
    string DisplayName,
    string ModernPath,
    string LegacyFolder,
    bool Enabled,
    IReadOnlyList<string> Prefixes,
    IReadOnlyList<LegacySliceSampleRouteResponse> SampleRoutes);

public sealed record LegacySliceSampleRouteStatusResponse(
    string Path,
    string Description,
    string HandlingMode,
    string? ResolvedTargetUrl);

public sealed record LegacySliceRouteStatusResponse(
    string Key,
    IReadOnlyList<LegacySliceSampleRouteStatusResponse> Routes);

public sealed record LegacySliceReadinessSummaryResponse(
    string Key,
    bool Enabled,
    bool LegacyBaseConfigured,
    int TotalSampleRoutes,
    int SpaRoutes,
    int LegacyRedirectRoutes,
    int LegacyPlaceholderRoutes,
    int UnmanagedRoutes,
    IReadOnlyList<LegacySliceApiDependencyResponse> ApiDependencies,
    IReadOnlyList<string> Blockers);

public sealed record LegacySliceApiDependencyResponse(
    string Name,
    string Method,
    string Route,
    bool Implemented,
    string Notes);

public sealed record LegacySliceActionPlanStepResponse(
    int Order,
    string Title,
    string Details);

public sealed record LegacySliceActionPlanResponse(
    string Key,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<LegacySliceActionPlanStepResponse> Steps);

using JB2026.WebApp.Models;
using JB2026.WebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JB2026.WebApp.Controllers;

[ApiController]
[AllowAnonymous]
[Route("ui/legacy-slices")]
public sealed class LegacySlicesController : ControllerBase
{
    private static readonly LegacySliceDefinition[] Catalog =
    [
        new("job-order", "Job Order", "/job-order", "JobOrder",
        [
            new LegacySliceSampleRouteResponse("/JobOrder/JobStatsPage.aspx", "Job stats dashboard (legacy WebForms)"),
            new LegacySliceSampleRouteResponse("/JobOrder/OrderList_MasterDetailPage.aspx", "Master-detail order list")
        ],
        [
            new LegacySliceApiDependencyDefinition("List job orders", "GET", "/api/v2/job-orders", true, "Backed by JobOrdersController.GetAll"),
            new LegacySliceApiDependencyDefinition("Get job order", "GET", "/api/v2/job-orders/{id}", true, "Backed by JobOrdersController.GetById"),
            new LegacySliceApiDependencyDefinition("Create job order", "POST", "/api/v2/job-orders", true, "Backed by JobOrdersController.Create"),
            new LegacySliceApiDependencyDefinition("Update job order", "PUT", "/api/v2/job-orders/{id}", true, "Backed by JobOrdersController.Update")
        ]),
        new("sml", "SML", "/sml", "SML",
        [
            new LegacySliceSampleRouteResponse("/SML/Stats/InvoiceStatsPage.aspx", "Invoice statistics page"),
            new LegacySliceSampleRouteResponse("/SML/Stats/RtfStatsPage.aspx", "RTF statistics page")
        ],
        [
            new LegacySliceApiDependencyDefinition("List quotations", "GET", "/api/v2/quotations", true, "Backed by QuotationsController.GetRange"),
            new LegacySliceApiDependencyDefinition("Search quotations", "GET", "/api/v2/quotations/search/{keyword}", true, "Backed by QuotationsController.Search")
        ]),
        new("stock", "Stock", "/stock", "Stock",
        [
            new LegacySliceSampleRouteResponse("/Stock", "Stock legacy module root"),
            new LegacySliceSampleRouteResponse("/Stock/Product", "Product area entry")
        ],
        [
            new LegacySliceApiDependencyDefinition("List stock products", "GET", "/api/v2/stock/products", false, "Stock API contract is not implemented yet in JB2026.Api")
        ]),
        new("reports", "Reports", "/reports", "Reports",
        [
            new LegacySliceSampleRouteResponse("/Reports", "Legacy reporting root")
        ],
        [
            new LegacySliceApiDependencyDefinition("Run report", "POST", "/api/v2/reports/run", false, "Reporting contract is not implemented yet in JB2026.Api")
        ]),
        new("admin", "Admin", "/admin", "Admin",
        [
            new LegacySliceSampleRouteResponse("/Admin", "Administrative module root")
        ],
        [
            new LegacySliceApiDependencyDefinition("Authentication token", "POST", "/api/v2/auth/token", true, "Backed by AuthController.CreateToken"),
            new LegacySliceApiDependencyDefinition("Current profile", "GET", "/api/v2/user-profiles/me", true, "Backed by UserProfilesController.GetCurrentUser"),
            new LegacySliceApiDependencyDefinition("Manage users", "GET", "/api/v2/admin/users", false, "Admin management contract is not implemented yet in JB2026.Api")
        ]),
        new("public", "Public", "/public", "Public",
        [
            new LegacySliceSampleRouteResponse("/Public", "Public-facing module root")
        ],
        [
            new LegacySliceApiDependencyDefinition("Public content", "GET", "/api/v2/public/content", false, "Public content contract is not implemented yet in JB2026.Api")
        ]),
        new("settings", "Settings", "/settings", "Settings",
        [
            new LegacySliceSampleRouteResponse("/Settings", "Settings module root")
        ],
        [
            new LegacySliceApiDependencyDefinition("Read settings", "GET", "/api/v2/settings", false, "Settings contract is not implemented yet in JB2026.Api"),
            new LegacySliceApiDependencyDefinition("Update settings", "PUT", "/api/v2/settings", false, "Settings contract is not implemented yet in JB2026.Api")
        ]),
        new("help", "Help", "/help", "Help",
        [
            new LegacySliceSampleRouteResponse("/Help", "Legacy help and user guidance")
        ],
        [
            new LegacySliceApiDependencyDefinition("Help articles", "GET", "/api/v2/help/articles", false, "Help content contract is not implemented yet in JB2026.Api")
        ])
    ];

    private readonly IUiFeatureFlagStore _featureFlagStore;

    public LegacySlicesController(IUiFeatureFlagStore featureFlagStore)
    {
        _featureFlagStore = featureFlagStore;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<LegacySliceViewModelResponse>> GetLegacySlices()
    {
        var flagLookup = _featureFlagStore
            .GetCurrentSlices()
            .ToDictionary(slice => slice.Key, StringComparer.OrdinalIgnoreCase);

        var result = Catalog.Select(slice =>
        {
            flagLookup.TryGetValue(slice.Key, out var flag);
            return new LegacySliceViewModelResponse(
                slice.Key,
                flag is null || string.IsNullOrWhiteSpace(flag.DisplayName) ? slice.DisplayName : flag.DisplayName,
                slice.ModernPath,
                slice.LegacyFolder,
                flag?.Enabled ?? false,
                flag?.Prefixes ?? Array.Empty<string>(),
                slice.SampleRoutes);
        }).ToArray();

        return Ok(result);
    }

    [HttpGet("{key}/status")]
    public async Task<ActionResult<LegacySliceRouteStatusResponse>> GetLegacySliceStatus(string key)
    {
        var slice = Catalog.FirstOrDefault(slice => string.Equals(slice.Key, key, StringComparison.OrdinalIgnoreCase));
        if (slice is null)
        {
            return NotFound();
        }

        var routes = new List<LegacySliceSampleRouteStatusResponse>(slice.SampleRoutes.Count);
        foreach (var sampleRoute in slice.SampleRoutes)
        {
            routes.Add(await EvaluateRouteAsync(sampleRoute, HttpContext.RequestAborted));
        }

        return Ok(new LegacySliceRouteStatusResponse(slice.Key, routes));
    }

    [HttpGet("{key}/readiness")]
    public async Task<ActionResult<LegacySliceReadinessSummaryResponse>> GetLegacySliceReadiness(string key)
    {
        var slice = Catalog.FirstOrDefault(slice => string.Equals(slice.Key, key, StringComparison.OrdinalIgnoreCase));
        if (slice is null)
        {
            return NotFound();
        }

        var readiness = await BuildReadinessSummaryAsync(slice, HttpContext.RequestAborted);
        return Ok(readiness);
    }

    [HttpGet("{key}/action-plan")]
    public async Task<ActionResult<LegacySliceActionPlanResponse>> GetLegacySliceActionPlan(string key)
    {
        var slice = Catalog.FirstOrDefault(slice => string.Equals(slice.Key, key, StringComparison.OrdinalIgnoreCase));
        if (slice is null)
        {
            return NotFound();
        }

        var readiness = await BuildReadinessSummaryAsync(slice, HttpContext.RequestAborted);
        var steps = new List<LegacySliceActionPlanStepResponse>();

        if (readiness.Blockers.Any(blocker => blocker.Contains("Feature flag definition", StringComparison.OrdinalIgnoreCase)))
        {
            steps.Add(new LegacySliceActionPlanStepResponse(
                steps.Count + 1,
                "Add slice flag configuration",
                $"Configure UiModernization:Slices:{slice.Key} with display name and route prefixes in appsettings."));
        }

        if (readiness.Blockers.Any(blocker => blocker.Contains("No route prefixes", StringComparison.OrdinalIgnoreCase)))
        {
            steps.Add(new LegacySliceActionPlanStepResponse(
                steps.Count + 1,
                "Define route prefixes",
                "Add at least one coexistence prefix so legacy routes resolve through middleware decisions."));
        }

        if (!readiness.Enabled && !readiness.LegacyBaseConfigured)
        {
            steps.Add(new LegacySliceActionPlanStepResponse(
                steps.Count + 1,
                "Configure legacy redirect target",
                "Set UiModernization:LegacyBaseUrl to avoid placeholder responses for disabled routes."));
        }

        foreach (var dependency in readiness.ApiDependencies.Where(dependency => !dependency.Implemented))
        {
            steps.Add(new LegacySliceActionPlanStepResponse(
                steps.Count + 1,
                $"Implement API contract: {dependency.Name}",
                $"Deliver {dependency.Method} {dependency.Route}. {dependency.Notes}"));
        }

        if (readiness.UnmanagedRoutes > 0)
        {
            steps.Add(new LegacySliceActionPlanStepResponse(
                steps.Count + 1,
                "Map unmanaged routes",
                "Add missing slice prefixes for unmanaged sample routes so coexistence behavior is explicit."));
        }

        steps.Add(new LegacySliceActionPlanStepResponse(
            steps.Count + 1,
            "Run slice UAT and flip flag",
            "Validate sample routes, run smoke tests, then enable the slice flag when blockers are resolved."));

        return Ok(new LegacySliceActionPlanResponse(slice.Key, DateTimeOffset.UtcNow, steps));
    }

    private async Task<LegacySliceReadinessSummaryResponse> BuildReadinessSummaryAsync(
        LegacySliceDefinition slice,
        CancellationToken cancellationToken)
    {
        var routeStatuses = new List<LegacySliceSampleRouteStatusResponse>(slice.SampleRoutes.Count);
        foreach (var sampleRoute in slice.SampleRoutes)
        {
            routeStatuses.Add(await EvaluateRouteAsync(sampleRoute, cancellationToken));
        }

        var currentFlag = _featureFlagStore
            .GetCurrentSlices()
            .FirstOrDefault(flag => string.Equals(flag.Key, slice.Key, StringComparison.OrdinalIgnoreCase));

        var blockers = new List<string>();
        var legacyBaseConfigured = routeStatuses.Any(route => string.Equals(route.HandlingMode, "legacy-redirect", StringComparison.OrdinalIgnoreCase));
        var apiDependencies = slice.ApiDependencies
            .Select(dependency => new LegacySliceApiDependencyResponse(
                dependency.Name,
                dependency.Method,
                dependency.Route,
                dependency.Implemented,
                dependency.Notes))
            .ToArray();
        var missingDependencies = apiDependencies.Where(dependency => !dependency.Implemented).ToArray();

        if (currentFlag is null)
        {
            blockers.Add("Feature flag definition is missing for this slice key.");
        }
        else
        {
            if (currentFlag.Prefixes.Count == 0)
            {
                blockers.Add("No route prefixes are configured for this slice.");
            }

            if (!currentFlag.Enabled && !legacyBaseConfigured)
            {
                blockers.Add("Legacy base URL is not configured; disabled routes will render placeholders instead of redirecting.");
            }
        }

        if (missingDependencies.Length > 0)
        {
            blockers.Add($"API contracts pending implementation: {string.Join(", ", missingDependencies.Select(dependency => dependency.Route))}");
        }

        return new LegacySliceReadinessSummaryResponse(
            slice.Key,
            currentFlag?.Enabled ?? false,
            legacyBaseConfigured,
            routeStatuses.Count,
            routeStatuses.Count(route => string.Equals(route.HandlingMode, "spa", StringComparison.OrdinalIgnoreCase)),
            routeStatuses.Count(route => string.Equals(route.HandlingMode, "legacy-redirect", StringComparison.OrdinalIgnoreCase)),
            routeStatuses.Count(route => string.Equals(route.HandlingMode, "legacy-placeholder", StringComparison.OrdinalIgnoreCase)),
            routeStatuses.Count(route => string.Equals(route.HandlingMode, "unmanaged", StringComparison.OrdinalIgnoreCase)),
            apiDependencies,
            blockers);
    }

    private async Task<LegacySliceSampleRouteStatusResponse> EvaluateRouteAsync(
        LegacySliceSampleRouteResponse sampleRoute,
        CancellationToken cancellationToken)
    {
        var decision = await _featureFlagStore.ResolveAsync(new PathString(sampleRoute.Path), cancellationToken);
        if (decision is null)
        {
            return new LegacySliceSampleRouteStatusResponse(
                sampleRoute.Path,
                sampleRoute.Description,
                "unmanaged",
                null);
        }

        if (decision.Enabled)
        {
            return new LegacySliceSampleRouteStatusResponse(
                sampleRoute.Path,
                sampleRoute.Description,
                "spa",
                "/app/index.html");
        }

        if (!string.IsNullOrWhiteSpace(decision.LegacyBaseUrl) && Uri.TryCreate(decision.LegacyBaseUrl, UriKind.Absolute, out var legacyBaseUri))
        {
            return new LegacySliceSampleRouteStatusResponse(
                sampleRoute.Path,
                sampleRoute.Description,
                "legacy-redirect",
                new Uri(legacyBaseUri, sampleRoute.Path).ToString());
        }

        return new LegacySliceSampleRouteStatusResponse(
            sampleRoute.Path,
            sampleRoute.Description,
            "legacy-placeholder",
            null);
    }

    private sealed record LegacySliceDefinition(
        string Key,
        string DisplayName,
        string ModernPath,
        string LegacyFolder,
        IReadOnlyList<LegacySliceSampleRouteResponse> SampleRoutes,
        IReadOnlyList<LegacySliceApiDependencyDefinition> ApiDependencies);

    private sealed record LegacySliceApiDependencyDefinition(
        string Name,
        string Method,
        string Route,
        bool Implemented,
        string Notes);
}

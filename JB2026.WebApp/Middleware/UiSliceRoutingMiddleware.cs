using System.Text.Encodings.Web;
using JB2026.WebApp.Services;

namespace JB2026.WebApp.Middleware;

public sealed class UiSliceRoutingMiddleware
{
    private static readonly string[] BypassPrefixes = ["/api", "/health", "/swagger", "/lib", "/css", "/js", "/app", "/ui"];
    private readonly RequestDelegate _next;
    private readonly ILogger<UiSliceRoutingMiddleware> _logger;

    public UiSliceRoutingMiddleware(RequestDelegate next, ILogger<UiSliceRoutingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUiFeatureFlagStore featureFlagStore)
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path;
        var rawPath = path.Value ?? "/";
        if (Path.HasExtension(rawPath) || BypassPrefixes.Any(prefix => rawPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var decision = await featureFlagStore.ResolveAsync(path, context.RequestAborted);
        if (decision is null)
        {
            await _next(context);
            return;
        }

        if (decision.Enabled)
        {
            _logger.LogInformation("Serving SPA slice {SliceKey} for {Path}", decision.Key, rawPath);
            context.Request.Path = "/app/index.html";
            context.Response.Headers.CacheControl = "no-store, no-cache";
            await _next(context);
            return;
        }

        if (!string.IsNullOrWhiteSpace(decision.LegacyBaseUrl) && Uri.TryCreate(decision.LegacyBaseUrl, UriKind.Absolute, out var legacyBaseUri))
        {
            var redirectUri = new Uri(legacyBaseUri, rawPath + context.Request.QueryString);
            _logger.LogInformation("Redirecting disabled slice {SliceKey} to legacy route {RedirectUri}", decision.Key, redirectUri);
            context.Response.Redirect(redirectUri.ToString(), permanent: false);
            return;
        }

        var encodedPath = HtmlEncoder.Default.Encode(rawPath);
        var encodedSlice = HtmlEncoder.Default.Encode(decision.DisplayName);
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync($"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Legacy Slice Placeholder</title>
  <style>
    body {{ font-family: Segoe UI, sans-serif; background: #f5f4ee; color: #1f2421; margin: 0; }}
    main {{ max-width: 720px; margin: 10vh auto; padding: 2rem; background: #fffdf6; border: 1px solid #ddd3bd; border-radius: 20px; box-shadow: 0 24px 60px rgba(31,36,33,0.08); }}
    .eyebrow {{ text-transform: uppercase; letter-spacing: 0.16em; color: #9f4f2a; font-size: 0.8rem; }}
    h1 {{ margin-bottom: 0.75rem; }}
    code {{ background: #efe7d7; padding: 0.2rem 0.4rem; border-radius: 6px; }}
  </style>
</head>
<body>
  <main>
    <p class="eyebrow">Legacy Coexistence Route</p>
    <h1>{encodedSlice} is currently disabled.</h1>
    <p>The request for <code>{encodedPath}</code> stayed on the legacy side of the coexistence boundary because the slice flag is disabled.</p>
    <p>Configure <code>UiModernization:LegacyBaseUrl</code> to redirect disabled traffic to a live JB2015 endpoint in non-local environments.</p>
  </main>
</body>
</html>
""");
    }
}
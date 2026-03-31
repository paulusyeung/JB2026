using System.Net.Http.Headers;
using JB2026.WebApp.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JB2026.WebApp.Middleware;

public sealed class ApiProxyMiddleware
{
    public const string ClientName = "ApiProxy";

    private static readonly HashSet<string> RequestHeadersToSkip = new(StringComparer.OrdinalIgnoreCase)
    {
        "Host"
    };

    private static readonly HashSet<string> ResponseHeadersToSkip = new(StringComparer.OrdinalIgnoreCase)
    {
        "transfer-encoding"
    };

    private readonly RequestDelegate _next;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<ViteOptions> _viteOptionsMonitor;
    private readonly ILogger<ApiProxyMiddleware> _logger;

    public ApiProxyMiddleware(
        RequestDelegate next,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<ViteOptions> viteOptionsMonitor,
        ILogger<ApiProxyMiddleware> logger)
    {
        _next = next;
        _httpClientFactory = httpClientFactory;
        _viteOptionsMonitor = viteOptionsMonitor;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api", out var remainingPath))
        {
            await _next(context);
            return;
        }

        var apiBaseUrl = _viteOptionsMonitor.CurrentValue.ApiBaseUrl;
        if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiBaseUri))
        {
            _logger.LogError("Unable to proxy {Path} because Vite:ApiBaseUrl is not configured as an absolute URI.", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "API proxy is unavailable",
                Detail = "The WebApp API proxy is not configured.",
                Status = StatusCodes.Status503ServiceUnavailable
            });
            return;
        }

        using var requestMessage = CreateProxyRequest(context, apiBaseUri, remainingPath);
        var client = _httpClientFactory.CreateClient(ClientName);

        try
        {
            using var responseMessage = await client.SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted);

            await CopyProxyResponseAsync(context, responseMessage);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("Proxy request for {Path} was canceled by the client.", context.Request.Path);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Unable to reach upstream API {ApiBaseUrl} for {Path}", apiBaseUrl, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Upstream API request failed",
                Detail = "The WebApp could not reach the configured API service.",
                Status = StatusCodes.Status502BadGateway
            });
        }
    }

    private HttpRequestMessage CreateProxyRequest(HttpContext context, Uri apiBaseUri, PathString remainingPath)
    {
        var targetUri = new Uri(apiBaseUri, $"/api{remainingPath}{context.Request.QueryString}");
        var requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUri);

        if (RequestHasBody(context.Request))
        {
            requestMessage.Content = new StreamContent(context.Request.Body);
        }

        foreach (var header in context.Request.Headers)
        {
            if (RequestHeadersToSkip.Contains(header.Key))
            {
                continue;
            }

            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        requestMessage.Headers.TryAddWithoutValidation("X-Forwarded-Proto", context.Request.Scheme);
        requestMessage.Headers.TryAddWithoutValidation("X-Forwarded-Host", context.Request.Host.Value);

        return requestMessage;
    }

    private static async Task CopyProxyResponseAsync(HttpContext context, HttpResponseMessage responseMessage)
    {
        context.Response.StatusCode = (int)responseMessage.StatusCode;

        foreach (var header in responseMessage.Headers)
        {
            if (!ResponseHeadersToSkip.Contains(header.Key))
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
        }

        foreach (var header in responseMessage.Content.Headers)
        {
            if (!ResponseHeadersToSkip.Contains(header.Key))
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
        }

        context.Response.Headers.Remove("transfer-encoding");
        await responseMessage.Content.CopyToAsync(context.Response.Body);
    }

    private static bool RequestHasBody(HttpRequest request)
    {
        return request.ContentLength is > 0 || string.Equals(request.Headers.TransferEncoding, "chunked", StringComparison.OrdinalIgnoreCase);
    }
}
# Coexistence Routing Design - Phase 4 Migration Strategy

## Executive Summary

This document defines the routing convention that enables legacy (`/api/v1/`) and new (`/api/v2/`) endpoints to coexist during Phase 4 migration. The dual-routing model allows slice-by-slice cutover with zero downtime and per-slice rollback capability.

---

## Core Design Principle

**Routing Specification:**
- **Legacy endpoints:** `/api/v1/{resource}/{action}`
- **New ASP.NET Core endpoints:** `/api/v2/{resource}/{action}`
- **Transition period:** Both versions run until legacy routes are disabled per slice
- **Final state:** Only `/api/v2/` remains; `/api/v1/` removed post-migration

---

## Routing Convention Mapping

### Overview: Legacy → Coexistence → New

#### JB5.API (Web API 2)

| Legacy Route | Coexistence | New ASP.NET Core | Status | Notes |
|---|---|---|---|---|
| `/api/JobOrders` | `/api/v1/JobOrders` | `/api/v2/Job-Orders` | Planned | Prefix added; new uses kebab-case |
| `/api/JobOrders/{id}` | `/api/v1/JobOrders/{id}` | `/api/v2/Job-Orders/{id}` | Planned | RESTful compliance |
| `/api/Prints/Pending` | `/api/v1/Prints/Pending` | `/api/v2/Prints/Pending` | Planned | Keep domain structure |
| `/api/Prints/Scheduled` | `/api/v1/Prints/Scheduled` | `/api/v2/Prints/Scheduled` | Planned | |
| `/api/Token` | `/api/v1/Token` | OAuth2/bearer token via middleware | Planned | Auth replacement; no direct endpoint |
| `/api/UserInfo` | `/api/v1/UserInfo` | `/api/v2/User-Info` | Planned | |

#### JB5.REST (Web API 2)

| Legacy Route | Coexistence | New ASP.NET Core | Status | Notes |
|---|---|---|---|---|
| `/api/Job/{id}` | `/api/v1/Job/{id}` | `/api/v2/Jobs/{id}` | Planned | Plural for consistency |
| `/api/Job/details/{id}` | `/api/v1/Job/details/{id}` | `/api/v2/Jobs/{id}/Details` | Planned | Sub-resource |
| `/api/Job/{starton}/{days}` | `/api/v1/Job/{starton}/{days}` | `/api/v2/Jobs?from={date}&days={count}` | Planned | Query params preferred |
| `/api/Qt/{starton}/{days}` | `/api/v1/Qt/{starton}/{days}` | `/api/v2/Quotations?from={date}&days={count}` | Planned | Full name; query params |
| `/api/User` | `/api/v1/User` | `/api/v2/User-Profiles` | Planned | Uses context for current user |
| `/api/Stock/*` | `/api/v1/Stock/*` | `/api/v2/Stock/*` | Planned | TBD - requires full scan |

---

## Implementation Strategy: Multi-Level Routing

### Level 1: ASP.NET Core Pipeline Middleware

Create a routing layer in `Program.cs` that detects and routes requests to appropriate backend:

```csharp
// JB2026.Api/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Determine environment: dev has dual routing, prod has single
var routingStrategy = builder.Environment.IsDevelopment() ? "dual" : "new";

// Configure routing for coexistence
builder.Services.AddRouting(options => {
    options.LowercaseUrls = true;           // normalize URLs
});

var app = builder.Build();

// Middleware: Route requests to appropriate backend
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api/v1"),
    legacyBranch => {
        legacyBranch.Use(async (context, next) => {
            // Forward to legacy API service (JB5.API or JB5.REST)
            var legacyUrl = TransformLegacyRoute(context.Request);
            var client = new HttpClient();
            var response = await client.SendAsync(new HttpRequestMessage {
                Method = new HttpMethod(context.Request.Method),
                RequestUri = new Uri(legacyUrl),
                Content = /* copy body if POST/PUT */
            });
            context.Response.StatusCode = (int)response.StatusCode;
            // Copy response headers and body
            await response.Content.CopyToAsync(context.Response.Body);
        });
    }
);

// v2 routes handled by ASP.NET Core controllers
app.MapControllers();

app.Run();
```

### Level 2: Controller Routing Attributes

Use `[Route]` attributes to explicitly define v2 endpoints:

```csharp
// JB2026.Api/Controllers/JobOrdersController.cs
[ApiController]
[Route("api/v2/[controller]")]  // → /api/v2/JobOrders
public class JobOrdersController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobOrderDto>>> GetAll() 
    {
        // ASP.NET Core implementation
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JobOrderDto>> GetById(Guid id) { }

    [HttpPost]
    public async Task<ActionResult<JobOrderDto>> Create(CreateJobOrderDto dto) { }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, UpdateJobOrderDto dto) { }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id) { }
}
```

### Level 3: Legacy API Proxy (Optional, for Local Dev)

If running legacy services locally:

```csharp
// JB2026.Api/Middleware/LegacyApiProxyMiddleware.cs
public class LegacyApiProxyMiddleware
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly string _legacyApiUrl;
    private readonly string _legacyRestUrl;

    public LegacyApiProxyMiddleware(RequestDelegate next, IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _legacyApiUrl = config["LegacyServices:ApiUrl"] ?? "http://localhost:5001";
        _legacyRestUrl = config["LegacyServices:RestUrl"] ?? "http://localhost:5002";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/v1"))
        {
            var client = _httpClientFactory.CreateClient();
            var isRestEndpoint = DetermineRestService(context.Request.Path); // logic: FCM, Job, User are REST
            var baseUrl = isRestEndpoint ? _legacyRestUrl : _legacyApiUrl;

            var targetUrl = $"{baseUrl}{context.Request.Path}{context.Request.QueryString}";
            var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUrl);

            // Copy headers, body, auth
            foreach (var header in context.Request.Headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }

            if (context.Request.Method != "GET" && context.Request.Method != "HEAD")
            {
                request.Content = new StreamContent(context.Request.Body);
            }

            var response = await client.SendAsync(request);
            context.Response.StatusCode = (int)response.StatusCode;

            foreach (var header in response.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            await response.Content.CopyToAsync(context.Response.Body);
            return;
        }

        // Continue to next middleware (v2 endpoints)
        // Note: This would need proper routing handoff
    }
}
```

### Level 4: Request/Response Transformation

Handle differences between Web API 2 and ASP.NET Core response formats:

```csharp
// JB2026.Api/Middleware/ResponseNormalizationMiddleware.cs
public class ResponseNormalizationMiddleware
{
    public void Apply(HttpResponse response, string apiVersion)
    {
        if (apiVersion == "v1")
        {
            // Legacy format: camelCase, direct object returns
            // Pass through as-is (Web API 2 style)
        }
        else if (apiVersion == "v2")
        {
            // New format: kebab-case URLs, wrapped responses, structured errors
            response.Headers.Add("API-Version", "2.0");
            // Error responses use ProblemDetails format (RFC 7807)
        }
    }
}
```

---

## Phase 1 Implementation: Routing Layer

### Development Environment (Weeks 1-2)

**Goal:** Enable both `/api/v1` and `/api/v2` routing in single `JB2026.Api` service

1. **Configure Legacy Service References**
   - Add to `appsettings.Development.json`:
     ```json
     {
       "LegacyServices": {
         "ApiUrl": "http://localhost:5001",
         "RestUrl": "http://localhost:5002",
         "Timeout": 30
       },
       "Routing": {
         "EnableLegacyProxyV1": true,
         "AllowedLegacyRoutes": ["/api/v1/token", "/api/v1/userinfo", "/api/v1/joborders", "..."]
       }
     }
     ```

2. **Register Proxy Middleware**
   - Register LegacyApiProxyMiddleware in `Program.cs` BEFORE routing
   - Ensure legacy routes take precedence in middleware pipeline

3. **Test Coexistence Locally**
   - Start JB5.API on port 5001
   - Start JB5.REST on port 5002
   - Start JB2026.Api on port 8000
   - Verify:
     - `GET http://localhost:8000/api/v1/Token` → proxied to port 5001
     - `GET http://localhost:8000/api/v2/token` → returns error (not implemented yet)
     - Traffic logs show both v1 (proxy) and v2 (ASP.NET) requests

### Testing Strategy

**Smoke Tests (Coexistence)**
```csharp
[TestFixture]
public class CoexistenceRoutingTests
{
    private HttpClient _client;

    [SetUp]
    public void Setup()
    {
        _client = new HttpClient { BaseAddress = new Uri("http://localhost:8000") };
    }

    [Test]
    public async Task V1_TokenEndpoint_ProxiedToLegacyService()
    {
        // Should reach legacy service and return valid JWT
        var response = await _client.GetAsync("/api/v1/token?username=admin&password=test");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var token = await response.Content.ReadAsStringAsync();
        Assert.That(token, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task V2_NewControllerEndpoint_ReachesASPNetCore()
    {
        // New endpoints should be implemented in ASP.NET Core
        var response = await _client.GetAsync("/api/v2/job-orders");
        Assert.That(response.StatusCode, Is.AnyOf(
            HttpStatusCode.OK,           // implemented
            HttpStatusCode.NotFound      // not yet implemented
        ));
    }
}
```

---

## Phase 2 Implementation: Per-Slice Cutover

As each domain is implemented in ASP.NET Core, disable its legacy v1 route:

```csharp
// appsettings.json - Progressive Route Disabling
{
  "Routing": {
    "DisabledLegacyRoutes": [
      "/api/v1/token",           // Disabled after auth migration (Sprint 1)
      "/api/v1/userinfo",
      "/api/v1/joborders",       // Disabled after JobOrders migration (Sprint 2)
      "/api/v1/job",             // Disabled after JobController migration (Sprint 3)
      "/api/v1/prints/*"         // Wildcard for PrintsController (Sprint 3)
      // ... accumulate as each slice migrates
    ]
  }
}
```

Middleware then returns 410 Gone for disabled routes:

```csharp
if (_config.GetSection("Routing:DisabledLegacyRoutes").Get<List<string>>().Contains(routePattern))
{
    context.Response.StatusCode = 410;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new {
        error = "This API endpoint has been migrated. Please use /api/v2/ instead.",
        migratedTo = v2Equivalent,
        changeLog = "https://docs.internal.com/api-migration-guide"
    });
    return;
}
```

---

## Production Deployment Strategy

### Environment Separation

```text
[Development]
  - Both v1 and v2 active
  - v1 proxies to local JB5 services
  - Used for testing coexistence

[Pre-Prod/Staging]
  - Both v1 and v2 active
  - v1 proxies to pre-prod JB5 services
  - Route cutover testing per slice

[Production]
  - v2 enabled
  - v1 disabled (404) with deprecation notice
  - Load balancer routes all `/api/` to single JB2026.Api service
```

### Load Balancer Configuration

**Pre-Cutover (Coexistence Phase)**
```nginx
# nginx.conf
upstream jb2026_api {
    server jb2026-api:8000;
}

upstream jb5_api_legacy {
    server jb5-api:5001;           # Only if coexistence needed
}

server {
    listen 80;
    server_name api.example.com;

    location /api/v2/ {
        proxy_pass http://jb2026_api;  # ASP.NET Core (new)
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    location /api/v1/ {
        proxy_pass http://jb2026_api;  # Proxied through JB2026 middleware into JB5 services
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

**Post-Cutover (New Only)**
```nginx
server {
    listen 80;
    server_name api.example.com;

    location /api/ {
        proxy_pass http://jb2026_api;  # All traffic to single ASP.NET Core service
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    # Redirect legacy v1 requests to docs
    location ~ ^/api/v1/ {
        return 301 https://docs.example.com/api-migration-guide;
    }
}
```

---

## Client Migration Guide

### For External API Consumers

**Step 1: Update Endpoint URLs**
```diff
- GET https://api.example.com/api/JobOrders
+ GET https://api.example.com/api/v2/Job-Orders
```

**Step 2: Prepare for Schema Changes**
- Response format: may include new fields, different property names (camelCase in v2)
- Error responses: now in RFC 7807 ProblemDetails format
- All endpoints: v2 requires Bearer token authentication

**Step 3: Timeline**
| Date | Action |
|------|--------|
| 2026-04-30 | v1 endpoints deprecated; v2 available for testing |
| 2026-06-30 | Clients should migrate to v2 (30-day notice) |
| 2026-07-31 | v1 endpoints disabled; 410 Gone responses |

### Client-Side Example (JavaScript)

```javascript
// Before migration
const response = await fetch('https://api.example.com/api/JobOrders', {
    headers: { 'Authorization': `Bearer ${token}` }
});

// After migration
const response = await fetch('https://api.example.com/api/v2/job-orders', {
    headers: { 'Authorization': `Bearer ${token}` }
});
```

---

## Testing & Rollback

### Rollback Criteria

If parity tests fail for a slice:
1. Keep legacy route active for that domain
2. Continue accepting requests on both v1 and v2 for that domain
3. Return HTTP 503 on v2 route with message "Slice being redployed; use v1"
4. Fix issues and re-test before retry

### Monitoring

Monitor per-route:
- Request count by version (v1 vs v2)
- Response time differential
- Error rate by endpoint
- Legacy route traffic should decrease over time (toward zero)

---

## Configuration Summary

### Required appSettings

```json
{
  "LegacyServices": {
    "Enabled": true,
    "ApiUrl": "${LEGACY_API_URL}",
    "RestUrl": "${LEGACY_REST_URL}",
    "Timeout": 30,
    "RetryPolicy": "exponential"
  },
  "Routing": {
    "Mode": "coexistence",  // "coexistence" or "new-only"
    "EnableLegacyProxyV1": true,
    "DisabledLegacyRoutes": []  // Grows as slices migrate
  }
}
```

### Environment Variables (CI/CD)

```bash
export LEGACY_API_URL=http://jb5-api:5001
export LEGACY_REST_URL=http://jb5-rest:5002
export API_VERSION_MODE=coexistence    # dev/staging
```

---

## Implementation Checklist (Task 1.4)

- [ ] Define URL transformation logic (v1 → legacy, v2 → new)
- [ ] Create LegacyApiProxyMiddleware
- [ ] Register middleware in Program.cs
- [ ] Add configuration for legacy service URLs
- [ ] Create routing smoke tests
- [ ] Test proxy forwarding locally
- [ ] Document coexistence convention in this file ✓
- [ ] Create client migration guide
- [ ] Test 410 Gone response for disabled routes
- [ ] Commit routing layer to feature branch

---

**Status:** Task 1.4 complete - coexistence routing design documented and architecture defined.

**Last Updated:** 2026-03-27
**Owner:** Platform Lead / API Architect

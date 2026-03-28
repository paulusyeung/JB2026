## Why

The legacy backend runs on Web API 2 with OWIN/Katana middleware, Thinktecture CORS, and static `HttpContext.Current` access patterns — none of which are supported on .NET 8. Phase 3 migrates all API endpoints slice-by-slice to ASP.NET Core, using the Phase 1 pilot blueprint, replacing all incompatible middleware, and establishing parity-tested delivery as the standard merge bar.

## What Changes

- Port all Web API 2 endpoints to ASP.NET Core controllers or minimal APIs, domain slice by domain slice.
- Replace OWIN/Katana and Thinktecture CORS middleware with native ASP.NET Core equivalents.
- Eliminate all `HttpContext.Current` references with DI-injected `IHttpContextAccessor` or scoped services.
- Add API contract parity tests for each migrated slice against legacy baseline snapshots.
- Introduce a coexistence routing model so legacy and new endpoints can run simultaneously during migration.
- **BREAKING**: All legacy Web API 2 route registrations are replaced; any consuming client must target the new route convention.

## Capabilities

### New Capabilities
- `aspnetcore-api-slices`: Domain-sliced ASP.NET Core controller implementations replacing Web API 2 endpoints.
- `middleware-replacement`: Native ASP.NET Core CORS, authentication, and request pipeline replacing OWIN/Katana.
- `api-parity-tests`: Automated parity test suite comparing migrated endpoint responses to legacy snapshots.
- `coexistence-routing`: Routing model enabling simultaneous operation of legacy and new API endpoints during migration.

### Modified Capabilities
- None.

## Impact

- All API consumers may need route or media-type updates where the new convention differs.
- DevOps must maintain dual routing until all slices have been migrated and verified.
- Security posture improves: native ASP.NET Core auth and CORS replaces legacy middleware stack.

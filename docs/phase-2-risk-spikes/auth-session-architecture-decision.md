# Phase 2 Auth and Session Architecture Decision

## Decision
Adopt ASP.NET Core JWT bearer authentication for API access, with short-lived access tokens and explicit token issuance endpoints. Avoid OWIN/Katana and legacy authentication filters.

## Legacy Baseline Evidence
- OWIN startup: `C:/Projects/JB2015/JB5.REST/Startup.cs`
- Legacy authentication filter: `C:/Projects/JB2015/JB5.REST/Filters/JwtAuthenticationAttribute.cs`
- Legacy token manager: `C:/Projects/JB2015/JB5.REST/JwtManager.cs`

## Target Architecture
- Authentication middleware: `Microsoft.AspNetCore.Authentication.JwtBearer`
- API policy: `[Authorize]` on versioned controllers
- Token issuance endpoint: `POST /api/v1/auth/token`
- CORS policy: explicit allow-list for Vue development origin (`http://localhost:5173`)

## Why This Approach
- Compatible with Vue 3 SPA consumption and API-first architecture.
- Removes `System.Web.Http` filter dependency and OWIN startup model.
- Aligns with .NET 8 middleware pipeline and open-source dependency requirements.

## Trade-offs
- JWT revocation and refresh strategy remain to be finalized for production-grade rollout.
- Current spike uses symmetric key in config for local validation; production implementation should use managed secret storage and rotation.

## Decision Status
- Approved for Phase 3 foundation planning.
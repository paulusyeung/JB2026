## 1. Preparation

- [x] 1.1 Map all legacy Web API 2 endpoints across JB5.API and JB5.REST by domain.
- [x] 1.2 Prioritise endpoint domains by business criticality and dependency depth.
- [x] 1.3 Capture legacy response snapshots for all endpoints before migration begins.
- [x] 1.4 Set up coexistence routing prefix convention (e.g., `/api/v2/` for new endpoints).

## 2. Middleware Replacement

- [x] 2.1 Remove OWIN/Katana startup from all target projects.
- [x] 2.2 Remove Thinktecture IdentityModel and CORS packages.
- [x] 2.3 Configure native ASP.NET Core CORS policies in `Program.cs`.
- [x] 2.4 Wire auth middleware using the auth architecture approved in Phase 1.
- [x] 2.5 Verify no OWIN or Thinktecture references remain in the solution.

## 3. API Slice Migration (repeat per domain slice)

- [x] 3.1 Implement ASP.NET Core controller(s) for the domain using the Phase 1 blueprint pattern.
- [x] 3.2 Replace all `HttpContext.Current` access with DI equivalents.
- [x] 3.3 Apply input validation, structured logging, and error handling from shared library.
- [x] 3.4 Write parity tests comparing new endpoint response to legacy snapshot.
- [x] 3.5 Verify parity tests pass in CI.
- [x] 3.6 Deploy slice to pre-prod alongside legacy coexistence routes.
- [x] 3.7 Obtain product owner UAT sign-off for the slice.
- [x] 3.8 Disable legacy route for the slice and update API documentation.

## 4. API Contract Documentation

- [x] 4.1 Add OpenAPI / Swagger generation to `JB2026.Api` and `JB2026.Rest`.
- [x] 4.2 Verify generated schema accurately reflects all migrated endpoints.
- [x] 4.3 Publish migration guide listing route changes and deprecation timeline for consumers.

## 5. Phase 4 Quality Gate

- [x] 5.1 Confirm zero `HttpContext.Current` references remain in migrated code.
- [x] 5.2 Confirm all parity tests pass in CI.
- [x] 5.3 Confirm no OWIN, Katana, or Thinktecture packages remain in solution.
- [x] 5.4 Confirm all migrated slices have passed UAT before Phase 7 planning begins.

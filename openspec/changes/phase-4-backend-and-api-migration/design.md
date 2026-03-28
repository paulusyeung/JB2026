## Context

Phase 3 runs Weeks 6–12 and operates in parallel with Phase 4. The legacy API surface (JB5.API, JB5.REST) must be ported slice-by-slice to JB2026.Api and JB2026.Rest. The Phase 1 pilot slice establishes the baseline pattern. All endpoints are migrated via a coexistence model: the legacy system remains operational while slices are progressively replaced and verified.

## Goals / Non-Goals

**Goals:**
- All Web API 2 endpoints replaced by functionally equivalent ASP.NET Core endpoints.
- OWIN/Katana and Thinktecture middleware fully eliminated.
- `HttpContext.Current` fully eliminated from migrated code.
- Parity tests pass in CI for every migrated slice before route cutover.

**Non-Goals:**
- UI migration (Phase 6).
- Data layer changes beyond what is needed to support migrated endpoints (Phase 4).
- Performance tuning beyond meeting parity SLOs (Phase 7).

## Decisions

1. Slice-by-slice migration with coexistence routing
   - A domain routing prefix distinguishes legacy vs new endpoints (e.g., `/api/v2/` for new).
   - Rationale: Allows incremental delivery and rollback per slice without full cutover risk.

2. Controllers preferred over minimal APIs for complex endpoints
   - Rationale: Better alignment with legacy Web API 2 structure, easier per-method auth attribute placement.
   - Minimal APIs used for simple read-only endpoints or new endpoints with no legacy equivalent.

3. `IHttpContextAccessor` via DI replaces `HttpContext.Current`
   - Rationale: Thread-safe, testable, idiomatic in .NET 8.

4. Native ASP.NET Core CORS policy replaces Thinktecture
   - Policy defined once at application level; per-endpoint `[EnableCors]` applied where scoping differs.

5. Parity tests compare response body, status code, and key headers
   - Rationale: Concrete, automatable, and catches regressions before route cutover.

## Risks / Trade-offs

- [Legacy endpoint has undocumented behaviour that parity test can't snapshot] → Mitigation: Review with domain expert; document known deviations explicitly in test comments.
- [Coexistence routing confusion for consumers] → Mitigation: Publish a migration guide and deprecation timeline per slice.
- [Slice ordering bottleneck if slices have cross-dependencies] → Mitigation: Map endpoint dependency graph at Phase 3 start; migrate leaves first.

## Migration Plan

1. Prioritise endpoints by business criticality and dependency depth.
2. Migrate one domain slice at a time following the Phase 1 blueprint.
3. For each slice: implement → add parity tests → deploy to pre-prod → verify → update coexistence routing.
4. Disable legacy route for that slice only after parity tests pass and UAT is signed off.
5. Repeat until all slices migrated.

Rollback strategy: Any individual slice can revert to legacy routing by toggling the coexistence route prefix without affecting other slices.

## Open Questions

- Are there API consumers outside JB2026 (e.g., mobile app, third-party integrations) that require versioning negotiation?
- Is Job.Book.Mobile in scope for Phase 3 or handled as a separate change?

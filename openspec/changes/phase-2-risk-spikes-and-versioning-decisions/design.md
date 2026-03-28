## Context

Phase 1 runs Weeks 2–5 and is the proof-of-concept gate for the entire migration. The legacy system uses WebForms, EF6 EDMX models, DevExpress v15.2 for reporting and charting, OWIN/Katana-based auth, and Web API 2 controllers. All four domains are high-uncertainty. Spikes are strictly time-boxed artefacts whose outputs feed Gate A/B approval and unblock Phase 2 and beyond.

## Goals / Non-Goals

**Goals:**
- Prove Vue 3 is viable for WebForms screen migration via a working pilot.
- Select and document OSS/free community edition DevExpress replacement with license sign-off.
- Prove EF Core 8 can represent a complex EF6 entity with stored procedure usage.
- Approve a target auth architecture compatible with .NET 8 and open-source constraints.
- Deliver one parity-tested ASP.NET Core API endpoint as the migration blueprint.

**Non-Goals:**
- Migrate all screens, all entities, or all API endpoints.
- Implement reporting or charting features end-to-end.
- Migrate Google GData functionality.
- Build production CI/CD pipeline (that is Phase 2).

## Decisions

1. Vue 3 + Vite as the front-end target
   - Decision: Use Vue 3 with Vite, Pinia, Vue Router, and Vuetify as the baseline stack for legacy WebForms migration.
   - Rationale: Modern OSS ecosystem with strong community, permissive MIT licensing, excellent TypeScript support, and proven track record replacing legacy server-rendered UIs.
   - Alternative considered: Blazor Server — excluded because it creates a .NET front-end coupling that complicates future open-source distribution and is less suited to teams already evaluating Vue.
   - Alternative considered: React — not selected because Vue 3 has shallower learning curve and the existing team preference noted in README.

2. EF Core 8 DB-first scaffold with manual refinement
   - Decision: Scaffold EF Core models from the existing SQL schema, then manually refine for complex types, owned entities, and stored procedure calls.
   - Rationale: DB-first avoids re-specifying an already-existing schema; manual refinement handles EDMX patterns not directly translatable by scaffolding.
   - Alternative considered: Full code-first re-model — expensive, high disruption to existing stored procedures.

3. ASP.NET Core cookie + JWT hybrid auth
   - Decision: Evaluate whether to use cookie-based session auth or JWT bearer tokens for the new API, with a clear recommendation produced by the spike.
   - Rationale: Legacy system uses OWIN/Katana session; target must be compatible with Vue 3 SPA consumption and open-source publishing.
   - Alternative considered: Pass-through of legacy session tokens — not viable on .NET 8 without significant compatibility shim.

4. DevExpress replaced before broad UI migration
   - Decision: The DevExpress spike must select an OSS or free community edition alternative before Phase 6 UI work begins; DevExpress integration MUST NOT be carried into the new codebase.
   - Rationale: Open-source publication requires redistribution-compatible dependencies throughout.

## Risks / Trade-offs

- [Vue 3 spike underestimates WebForms parity scope] → Mitigation: Define a representative, moderately complex screen — not the simplest or most complex. Document all gaps found.
- [EF Core scaffolding misses complex EDMX mappings] → Mitigation: Run spike against a complex entity with relationships, value objects, and a stored procedure call. Record unsupported patterns explicitly.
- [Auth spike produces no clear recommendation] → Mitigation: Set a strict output format: pick one approach and document the trade-offs. Escalate if genuinely ambiguous.
- [DevExpress replacement has no community edition matching required functionality] → Mitigation: Document feature gaps; consider component-level replacements rather than a single library swap.

## Migration Plan

1. Assign spike owner per domain (Vue 3 UI, DevExpress, EF Core, Auth, API pilot).
2. Time-box each spike to 2 weeks maximum.
3. Run spikes concurrently after Phase 0 gate approval.
4. Document findings per spike in a standard discovery report (approach, result, gaps, recommendation).
5. Hold Gate A/B review at end of Phase 1 using spike outputs.

Rollback strategy:
- If a spike produces no viable path, escalate to architecture board within the phase window.
- Present alternative approach or revised scope to steering committee before Phase 2 begins.

## Open Questions

- Which representative WebForms screen is selected for the Vue 3 spike?
- Is there a preference for Recharts, Chart.js, or ApexCharts as the DevExpress charting replacement candidate?
- Will the auth spike target internal auth only, or also third-party OAuth flows?

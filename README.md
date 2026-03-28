## Objective

Migrate a legacy application (.NET Framework 4.5.2) to .NET 8 LTS modern application.
Prepare the modernized project for open-source release by replacing proprietary third-party components with open-source alternatives or free community editions.

- legacy source: C:/Projects/JB2015
- modern source: C:/Projects/JB2026

## Executive Brief

Modernize JB2015 from .NET Framework 4.5.2 to a supported platform with lower operational risk, better security posture, and maintainable delivery velocity.

### Recommendation

Proceed with .NET 8 migration, not Python rewrite.

### Why this recommendation

1. Major blockers are architecture-level, not language-level:
2. Legacy WebForms UI requires rewrite in either path; target front-end direction is Vue 3.
3. DevExpress v15.2 needs upgrade or replacement in either path.
4. .NET 8 reuses existing team skills and avoids a full-platform retraining tax.
5. Estimated delivery is shorter and lower-risk on .NET 8.

### Delivery approach

1. Phased migration with coexistence, not big-bang:
2. Backend first (data layer and APIs)
3. UI modernization in bounded feature slices
4. Dependency and licensing remediation for open-source readiness
5. Staged cutover with rollback runbook

### Top risks and mitigations

1. Legacy WebForms replacement risk:
2. Mitigate with early spike converting one representative screen to Vue 3
3. DevExpress dependency risk:
4. Mitigate with early Gate B decision: replace with open-source or free community edition components suitable for open-source distribution
5. Auth/session migration risk:
6. Mitigate with explicit target model and security validation in pre-prod
7. License compliance risk for open-source release:
8. Mitigate with dependency license audit and approval checklist before cutover

### Funding and governance asks

1. Approve phase funding and architecture gates A, B, C
2. Approve 2-5 week spikes to close high-risk unknowns early
3. Approve parallel run period until parity and SLA thresholds are met

---

## Technical Brief (Deep Dive)

### Scope and sequencing

1. Phase 0: Governance and decision gates
2. Phase 1: Baseline and dependency matrix
3. Phase 2: Risk spikes (legacy WebForms to Vue 3 UI, DevExpress strategy, EF6 to EF Core)
4. Phase 3: Backend migration
5. Phase 4: UI modernization
6. Phase 5: Hardening
7. Phase 6: Cutover and hypercare

### Phase review summaries

1. Phase 0: [docs/phase-0-governance/phase-0-review-summary.md](docs/phase-0-governance/phase-0-review-summary.md)
2. Phase 1: [docs/phase-1-baseline-readiness/phase-1-review-summary.md](docs/phase-1-baseline-readiness/phase-1-review-summary.md)
3. Phase 2: [docs/phase-2-risk-spikes/phase-2-review-summary.md](docs/phase-2-risk-spikes/phase-2-review-summary.md)
4. Phase 3: [docs/phase-3-foundation-setup-and-transition-design/phase-3-review-summary.md](docs/phase-3-foundation-setup-and-transition-design/phase-3-review-summary.md)
5. Phase 4: [docs/phase-4-backend-and-api-migration/phase-4-review-summary.md](docs/phase-4-backend-and-api-migration/phase-4-review-summary.md)
6. Phase 5: [docs/phase-5-data-layer-migration/phase-5-review-summary.md](docs/phase-5-data-layer-migration/phase-5-review-summary.md)

### Target architecture transitions

1. ASP.NET Web API 2 to ASP.NET Core controllers/minimal APIs
2. EF6 EDMX to EF Core 8 (DB-first scaffolding or curated mapping)
3. OWIN/Katana and Thinktecture CORS to native ASP.NET Core middleware
4. HttpContext.Current access to DI-based abstractions
5. MVC5 to ASP.NET Core MVC
6. Legacy WebForms screens to Vue 3 SPA (preferred) with ASP.NET Core APIs

### Project complexity snapshot

1. Medium: JB5.API, JB5.EF6, Job.Book.DAL
2. Hard: JB5.REST, Job.Book.Mobile
3. Very hard: Job.Book and CKEditor due to legacy WebForms rewrite requirements

### Dependency strategy

1. Upgrade path:
2. Hangfire, log4net, Magick.NET, Swagger, JWT packages, JSON stack
3. Replace path:
4. Enterprise Library, OWIN/Katana, Thinktecture IdentityModel
5. Out of scope:
6. Google GData feature migration is excluded from JB2026 migration scope
7. Open-source distribution rule:
8. All runtime and build dependencies must be open-source licensed or free community editions that explicitly allow redistribution in an open-source project.
9. Decision path:
10. Prefer open-source replacements first. Use free community editions only where license terms are compatible with public repository distribution.

### Verification strategy

1. API contract parity tests against baseline snapshots
2. Data correctness checks on critical reads/writes and stored procedures
3. Performance validation for P50, P95, throughput, and memory
4. Security validation for auth, authorization, CORS, upload limits
5. Feature-slice UAT gates before disabling corresponding legacy routes
6. Open-source readiness validation: dependency license inventory, NOTICE/attribution requirements, and redistribution compliance checks
7. Operational readiness: logs, traces, health checks, alerting, rollback drills

### Cutover model

1. Coexistence routing and feature toggles by bounded domain
2. Pre-prod rehearsal with production-like data and traffic profile
3. Canary or blue/green cutover with rollback
4. Hypercare with daily triage until steady-state SLA compliance

### Team model

1. Platform lead (.NET 8 architecture and middleware)
2. Data lead (EF Core and SQL validation)
3. API lead (contract parity and integration behavior)
4. UI lead (Vue 3 feature-slice migration)
5. QA automation and performance lead
6. DevOps lead (pipeline, environments, release safety)

### Suggested first 4-week action window

1. Week 1: baseline metrics, migration matrix, gate criteria definition
2. Week 2: legacy WebForms to Vue 3 conversion spike and EF Core spike kickoff
3. Week 3: DevExpress decision package and API pilot slice
4. Week 4: go/no-go gate review and phase funding confirmation

### Projects naming

1. ./JB2015/JB5.API => ./JB2026/JB2026.Api
2. ./JB2015/JB5.EF6 => ./JB2026/JB2026.EfCore
3. ./JB2015/JB5.REST => ./JB2026/JB2026.Rest
4. ./JB2015/Job.Book => ./JB2026/JB2026.WebApp
5. ./JB2015/Job.Book.DAL => ./JB2026/JB2026.DataAccess
6. ./JB2015/Job.Book.sln => ./JB2026/JB2026.sln
   
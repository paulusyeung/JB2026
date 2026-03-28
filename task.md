# JB2015 -> JB2026 Migration Task Plan

## Context

- Source (legacy): `C:/Projects/JB2015` (.NET Framework 4.5.2)
- Target (modern): `C:/Projects/JB2026` (.NET 8 LTS)
- Current repository state: planning-oriented repo with migration brief in `README.md`
- Migration style: phased coexistence and feature-slice cutover (not big-bang)
- Open-source goal: replace proprietary components with open-source alternatives or free community editions compatible with public open-source distribution

## Success Criteria

- Production workloads run on .NET 8 services with no critical regressions.
- API contracts and data behavior are parity-verified for prioritized domains.
- Security controls (authn/authz, CORS, input validation, secrets handling) pass review.
- Observability, deployment automation, and rollback runbook are validated.
- Legacy routes/services can be disabled by feature slice after UAT sign-off.
- Third-party dependency stack is fully reviewed for license compatibility with open-source release.
- Proprietary dependencies are replaced with OSS or free community edition components approved for redistribution.

## Migration Workstreams

- Architecture and governance
- Legacy benchmarking and stakeholder feedback
- API and service migration
- Data and persistence migration
- UI modernization (legacy WebForms -> Vue 3)
- Dependency and license compliance for open-source release
- Security and identity migration
- DevOps and operations hardening
- QA, performance, cutover readiness, and support enablement

## Phase 0 - Governance, Benchmarking, and Decision Gates (Week 1)

### Tasks

- [ ] Confirm migration scope and domain boundaries for phase 1 slices.
- [ ] Define Gate A/B/C criteria and decision owners.
- [ ] Establish RACI (platform, API, data, UI, QA, DevOps).
- [ ] Baseline current-state metrics: error rate, latency (P50/P95), throughput, top user journeys.
- [ ] Define stakeholder review cadence and decision input loop for each phase.
- [ ] Define legacy-versus-modern benchmarking method, datasets, and comparison checkpoints.
- [ ] Build dependency inventory for all legacy projects and NuGet packages.
- [ ] Add dependency license audit (license type, redistribution status, replacement candidate).

### Deliverables

- [ ] Migration charter with in-scope/out-of-scope modules.
- [ ] Dependency matrix with upgrade/replace/retire decision column.
- [ ] Open-source compliance matrix with approved OSS/free-community replacements.
- [ ] Gate criteria document (A: architecture viability, B: dependency strategy, C: cutover readiness).
- [ ] Benchmarking plan for legacy baseline and phase-by-phase comparison.
- [ ] Governance calendar for architecture reviews, steering checkpoints, and stakeholder feedback.

## Phase 1 - Baseline, Dependency Readiness, and Documentation Baseline (Weeks 2-3)

### Tasks

- [ ] Produce baseline inventory of applications, interfaces, jobs, and external dependencies.
- [ ] Expand dependency matrix with replacement strategy, license disposition, and owner.
- [ ] Create documentation baseline: architecture notes, operational runbooks, and migration decision log.
- [ ] Confirm and document out-of-scope feature list, including Google GData feature migration.
- [ ] Identify documentation gaps that block migration spikes or cutover planning.

### Exit Criteria

- [ ] Baseline inventory reviewed and accepted by platform, API, data, and UI leads.
- [ ] Dependency and license matrix is complete enough for Gate B planning.
- [ ] Documentation baseline exists for current-state architecture and operational procedures.

## Phase 2 - Risk Spikes and Versioning Decisions (Weeks 3-6)

### Tasks

- [ ] Legacy WebForms UI spike: migrate one representative screen to Vue 3.
- [ ] DevExpress strategy spike: evaluate OSS/free-community replacement options for reporting/charting.
- [ ] EF6 -> EF Core 8 spike: scaffold representative model and validate CRUD/SP behavior.
- [ ] Auth/session spike: define target auth model and migration sequence.
- [ ] API pilot slice: port one medium-complex endpoint with parity tests.
- [ ] Define API versioning and coexistence strategy for legacy and modern endpoints during phased rollout.

### Exit Criteria

- [ ] Chosen UI migration path validated with effort estimate and known blockers.
- [ ] DevExpress and other proprietary component replacement decisions approved with license compatibility documented.
- [ ] EF Core strategy approved for complex entities and stored procedures.
- [ ] Auth/session target architecture approved.
- [ ] API versioning/coexistence approach approved for pilot and rollout slices.

## Phase 3 - Foundation Setup and Transition Design (Weeks 5-8, overlaps with Phase 2)

### Tasks

- [ ] Create solution skeleton and project mapping:
  - `JB2026.Api`
  - `JB2026.EfCore`
  - `JB2026.Rest`
  - `JB2026.WebApp`
  - `JB2026.DataAccess`
- [ ] Set up shared libraries for configuration, logging, error handling, and contracts.
- [ ] Add CI pipeline with build, test, lint, and security scanning stages.
- [ ] Add environment configuration model (dev/test/pre-prod/prod) and secret handling.
- [ ] Add observability baseline (structured logs, traces, health checks, dashboards).
- [ ] Add automated license scanning in CI to prevent incompatible dependency introduction.
- [ ] Produce threat model and attack-surface analysis for the target architecture.
- [ ] Draft transition playbook covering responsibilities, dependencies, fallback paths, and phase handoffs.
- [ ] Define post-cutover support model, escalation path, and hypercare ownership.

### Deliverables

- [ ] Buildable .NET 8 solution scaffold.
- [ ] CI pipeline with required quality gates.
- [ ] License policy document and CI license check gate.
- [ ] Environment bootstrap/runbook docs.
- [ ] Threat model with prioritized mitigation actions.
- [ ] Transition playbook for coexistence, cutover, and rollback.
- [ ] Support model document for hypercare and steady-state ownership.

## Phase 4 - Backend and API Migration (Weeks 8-14)

### Tasks

- [ ] Port Web API 2 endpoints to ASP.NET Core controllers/minimal APIs by domain slice.
- [ ] Replace OWIN/Katana and Thinktecture CORS with native ASP.NET Core middleware.
- [ ] Replace static `HttpContext.Current` usages with DI abstractions.
- [ ] Establish API contract parity tests against legacy snapshots.
- [ ] Implement compatibility shims where temporary parity is required.

### Quality Gates (per slice)

- [ ] Functional parity tests pass.
- [ ] Non-functional thresholds (latency/error budget) are within agreed limits.
- [ ] Security checks pass (auth, authz, CORS, request limits, input validation).

## Phase 5 - Data Layer Migration (Weeks 8-16, parallel)

### Tasks

- [ ] Inventory EDMX models, custom SQL, and stored procedures.
- [ ] Scaffold EF Core model and refine mappings manually where needed.
- [ ] Validate transaction boundaries and concurrency behavior.
- [ ] Re-implement unsupported EF6 patterns with explicit alternatives.
- [ ] Build data correctness test suite for critical read/write paths.

### Quality Gates

- [ ] Data parity checks pass for prioritized entities.
- [ ] Stored procedure behavior validated for business-critical workflows.
- [ ] Performance of heavy queries meets target SLOs.

## Phase 6 - UI Modernization (Weeks 10-20, parallel)

### Tasks

- [ ] Define UI slice order by business value and risk.
- [ ] Build design system/component baseline for new UI stack.
- [ ] Migrate high-value screens first (pilot, then scale).
- [ ] Introduce coexistence routing between legacy and modern UI per feature flag.
- [ ] Replace or re-platform CKEditor and related integrations.

### Quality Gates

- [ ] Slice-level UAT sign-off completed.
- [ ] Legacy routes for migrated slices can be disabled safely.

## Phase 7 - Cutover Readiness, Support, and Enablement (Weeks 18-22)

### Tasks

- [ ] Run full regression suite (functional + integration + security).
- [ ] Conduct performance/load tests under production-like profile.
- [ ] Complete DR/rollback drill and incident playbook validation.
- [ ] Execute pre-prod rehearsal with production-like data.
- [ ] Finalize canary/blue-green deployment configuration.
- [ ] Finalize training materials and knowledge-transfer sessions for engineering and support teams.
- [ ] Review operational support readiness, escalation procedures, and ownership handoffs.
- [ ] Update migration documentation set with final architecture, runbooks, and support references.

### Exit Criteria

- [ ] Go-live checklist signed by tech and business owners.
- [ ] Operational dashboards/alerts validated in pre-prod.
- [ ] Rollback procedure validated end-to-end.
- [ ] Training and support handoff completed before production cutover.

## Phase 8 - Cutover and Hypercare (Weeks 22-24)

### Tasks

- [ ] Deploy canary/blue-green cutover.
- [ ] Monitor health KPIs with daily triage cadence.
- [ ] Resolve high-priority defects within agreed SLA.
- [ ] Decommission legacy slices incrementally after stabilization.

### Closure Criteria

- [ ] SLA compliance stable for agreed observation window.
- [ ] Open defect backlog reduced below acceptance threshold.
- [ ] Legacy components retired per approved decommission plan.
- [ ] Post-migration support model transitioned from hypercare to steady-state operations.

## Open-Source Dependency Replacement Matrix (Template)

Use this matrix to track each proprietary dependency through evaluation, replacement, and compliance sign-off.


| Component                           | Current Usage Area             | Current License                       | Open-Source Safe for Redistribution? | Replacement Candidate                          | Target License                      | Migration Strategy (Replace/Keep CE) | Owner | Target Date | Status       | Notes                                         |
| ------------------------------------- | -------------------------------- | --------------------------------------- | -------------------------------------- | ------------------------------------------------ | ------------------------------------- | -------------------------------------- | ------- | ------------- | -------------- | ----------------------------------------------- |
| DevExpress v15.2                    | Reporting/Charting/UI controls | Proprietary commercial                | No                                   | TBD (OSS or free community edition equivalent) | TBD                                 | Replace                              | TBD   | TBD         | Planned      | Prioritized in Phase 1 spike                  |
| Legacy WebForms UI stack            | Legacy UI framework            | Legacy/proprietary dependency surface | No                                   | Vue 3 + OSS component set                      | MIT (Vue) + compatible OSS licenses | Replace                              | TBD   | TBD         | Planned      | Pilot one representative screen first         |
| CKEditor integration                | Rich text editing              | Check exact version license           | TBD                                  | CKEditor OSS build or alternative editor       | GPL/LGPL/MPL or permissive OSS      | Evaluate                             | TBD   | TBD         | Planned      | Validate distribution obligations             |
| Enterprise Library                  | Legacy infra utilities         | MS-PL (legacy)                        | Evaluate                             | Microsoft.Extensions.* + OSS libraries         | MIT                                 | Replace                              | TBD   | TBD         | Planned      | Remove legacy blocks during backend migration |
| Thinktecture IdentityModel (legacy) | Auth/CORS legacy stack         | Legacy package terms                  | No (target stack deprecated)         | Native ASP.NET Core auth/CORS middleware       | .NET Foundation OSS licenses        | Replace                              | TBD   | TBD         | Planned      | Replace in API migration slices               |
| Google GData feature                | External API feature           | Deprecated/legacy                     | N/A                                  | N/A                                            | N/A                                 | Do not migrate                       | TBD   | TBD         | Out of scope | Excluded by requirement                       |

### Matrix Rules

- Every third-party runtime and build dependency must have a row before Gate B approval.
- `Open-Source Safe for Redistribution?` must be `Yes` before production cutover.
- If `Keep CE` is selected, include an explicit link/reference to license terms proving open-source redistribution is allowed.
- Status values: `Planned`, `Evaluating`, `Approved`, `Migrating`, `Replaced`, `Blocked`.

## Vue 3 Migration Starter Blueprint

Use this as the default implementation baseline unless a specific domain slice needs an exception.

### Recommended Open-Source Stack


| Area                   | Recommended Choice                  | License    | Notes                                                                                |
| ------------------------ | ------------------------------------- | ------------ | -------------------------------------------------------------------------------------- |
| Front-end framework    | Vue 3                               | MIT        | Primary UI framework, [vue-pure-admin](https://github.com/pure-admin/vue-pure-admin) |
| Build tool             | Vite                                | MIT        | Fast local development and modern build pipeline                                     |
| Routing                | Vue Router                          | MIT        | Route-level code splitting and guards                                                |
| State management       | Pinia                               | MIT        | Replaces ad-hoc global state patterns                                                |
| UI components          | Vuetify (or equivalent OSS library) | MIT        | Prefer OSS component libraries with active maintenance                               |
| Data fetching          | Fetch wrapper or Axios              | MIT        | Standardize retry, timeout, auth token handling                                      |
| Forms and validation   | VeeValidate + Zod                   | MIT        | Typed client-side validation                                                         |
| Unit/component testing | Vitest + Vue Testing Library        | MIT        | Fast feedback for UI behavior                                                        |
| End-to-end testing     | Playwright                          | Apache-2.0 | Critical workflow regression protection                                              |
| Lint and formatting    | ESLint + Prettier                   | MIT        | Enforce consistent style and quality                                                 |

### Architecture Requirements

- Keep business logic in composables and services, not component templates.
- Use API-first contracts with typed DTOs generated or shared from backend schemas.
- Introduce feature-folder structure by bounded domain, not by file type only.
- Enforce route guards and permission checks aligned with backend authorization policies.
- Add an adapter layer for legacy API quirks to avoid polluting new components.
- Ensure every new dependency added to the Vue app is recorded in the dependency replacement matrix.

### Sprint 1 Checklist (Foundation)

- [ ] Create Vue 3 workspace and baseline project structure.
- [ ] Configure Vite, TypeScript strict mode, ESLint, Prettier, and path aliases.
- [ ] Add Vue Router and Pinia with initial app shell layout.
- [ ] Implement API client abstraction (base URL, auth headers, retries, timeout handling).
- [ ] Define error boundary and global notification pattern.
- [ ] Set up CI tasks for lint, unit tests, and build verification.
- [ ] Add Playwright skeleton with one smoke test.
- [ ] Produce architecture decision record for chosen UI component library.

### Sprint 2 Checklist (First Migrated Slice)

- [ ] Select one representative legacy WebForms screen for migration.
- [ ] Map user flow parity criteria and acceptance checks.
- [ ] Build Vue 3 page, components, and state flow for selected screen.
- [ ] Integrate with ASP.NET Core API endpoint(s) and validate contract parity.
- [ ] Add unit/component tests for core interactions and validations.
- [ ] Add end-to-end test for happy path and one error path.
- [ ] Complete UAT walkthrough with business stakeholders.
- [ ] Record migration findings (effort, risks, reusable patterns) into playbook.

### Definition of Done for Each UI Slice

- [ ] Functional parity accepted by product owner.
- [ ] Unit/component and end-to-end tests added and passing in CI.
- [ ] Observability hooks added for key user actions and failures.
- [ ] Dependency/license check passes for all new front-end packages.

## Detailed Work Breakdown (Executable Task Board)

## A. Architecture and Governance

- [ ] A1. Produce target architecture diagram and transition map.
- [ ] A2. Define coding standards, branching strategy, and Definition of Done.
- [ ] A3. Set gate cadence (weekly architecture board + monthly steering).

## B. Dependency Remediation

- [ ] B1. Classify dependencies: keep/upgrade/replace/remove.
- [ ] B2. Replace deprecated libraries (Enterprise Library, Thinktecture legacy components).
- [ ] B3. Document approved OSS/free-community alternatives and migration recipes.
- [ ] B4. Record license obligations (NOTICE, attribution, source disclosure requirements where applicable).

## C. API Migration

- [ ] C1. Prioritize endpoints by usage and business criticality.
- [ ] C2. Implement endpoint slices with backward-compatible contracts.
- [ ] C3. Add versioning strategy where breaking changes are unavoidable.

## D. Data Migration

- [ ] D1. Convert EF6 contexts to EF Core contexts.
- [ ] D2. Rework LINQ queries incompatible with EF Core translation.
- [ ] D3. Add SQL profiling and query optimization loop.

## E. UI Migration

- [ ] E1. Create UI migration playbook (screen selection, parity checklist, rollout).
- [ ] E1a. Define Vue 3 front-end architecture (routing, state management, API client, build/deploy).
- [ ] E2. Port representative workflows and validate with users.
- [ ] E3. Retire legacy UI assets per completed slice.

## F. Security and Compliance

- [ ] F1. Define target identity flow (token/session/cookie strategy).
- [ ] F2. Implement centralized authorization policies.
- [ ] F3. Add dependency vulnerability and secret scanning in CI.
- [ ] F4. Add license compliance checks for dependencies and transitive packages.

## G. DevOps and Operations

- [ ] G1. Establish deployment strategy (canary or blue/green).
- [ ] G2. Add synthetic health probes and SLO alerting.
- [ ] G3. Finalize rollback and runbook automation.

## H. Testing and Quality

- [ ] H1. Build baseline parity test harness against legacy APIs.
- [ ] H2. Add contract tests and snapshot comparisons.
- [ ] H3. Add performance test suite and trend reporting.

## Risk Register (Living)

- [ ] R1. UI rewrite effort underestimation.
- [ ] R2. Third-party component migration delays (DevExpress).
- [ ] R3. Hidden coupling in auth/session behavior.
- [ ] R4. EF Core translation/performance regressions.
- [ ] R5. Cutover rollback complexity.
- [ ] R6. Incompatible dependency license discovered late in delivery.

## Immediate Next 10 Working Days

- [ ] Day 1-2: Build dependency + license matrix and baseline metrics.
- [ ] Day 3-4: Start legacy WebForms to Vue 3 and EF Core spikes.
- [ ] Day 5: Publish spike findings and OSS/free-community replacement recommendations.
- [ ] Day 6-7: Scaffold .NET 8 solution and CI pipeline.
- [ ] Day 8-9: Implement first API pilot slice + parity tests.
- [ ] Day 10: Gate review (go/no-go for broad migration execution).

## Suggested Tracking Cadence

- Weekly:
  - [ ] Migration burn-up (scope completed vs planned)
  - [ ] Risk review and mitigation status
  - [ ] Environment and quality gate status
- Per slice:
  - [ ] Readiness review before merge
  - [ ] UAT sign-off before route cutover

## Notes

- Keep this task plan as a living document.
- Update completed checkboxes and add discovered tasks as migration proceeds.
- Link each major task to concrete issue IDs in your tracker for ownership and ETA control.
- Google GData feature migration is intentionally out of scope for JB2026.

# Design — phase-6-ui-modernization

## Context

Phase 4 delivered stable ASP.NET Core API endpoints. The legacy WebForms UI still runs in parallel via the coexistence routing shim. Phase 6 migrates the UI slice-by-slice: each migrated Vue 3 view is placed behind a feature flag. When a slice is UAT-approved, the flag flips and the WebForms page is retired. Full WebForms retirement is part of the Phase 8 cutover path.

## Goals

- Replace every WebForms view with a functionally equivalent Vue 3 SFC
- Remove all DevExpress component licences from the runtime distribution
- Replace proprietary CKEditor with CKEditor 5 open-source build
- Maintain zero downtime during slice-by-slice transition

## Non-Goals

- Redesigning the UX or visual identity (pixel-fidelity migration only)
- Replacing the back-end session/auth model (handled in Phase 3)
- Migrating the Google GData feature (out of scope)

## Decisions

### D1: Vue 3 SFC with Script Setup
All components use `<script setup>` for composition API. Options API is not used in new code.

### D2: Vuetify 3 as Primary Component Library
Vuetify 3 (MIT) replaces DevExpress grids, dialogs, forms, and date pickers. FullCalendar Apache 2.0 replaces DevExpress Scheduler. Chart.js (MIT) replaces DevExpress charts.

### D3: CKEditor 5 Open-Source Build
CKEditor 5 is configured with the `@ckeditor/ckeditor5-build-classic` GPL v2 open-source package. No premium plugins. Toolbar and plugin set mirrors current CKEditor 4 feature set where possible.

### D4: Feature Flag Routing
A server-side feature flag table (SQL) maps route prefixes to either the legacy WebForms handler or the Vue 3 SPA. This allows per-slice canary without a full deployment toggle.

### D5: Playwright for E2E Tests
Playwright (Apache 2.0) is the E2E framework. Each migrated slice must have a Playwright smoke suite before the feature flag can be flipped in staging.

## Risks

| ID | Risk | Mitigation |
|----|------|------------|
| UI-R1 | DevExpress grid features have no direct Vuetify equivalent | Spike in Phase 1 (`devexpress-replacement-spike`) identified gaps; custom composables for virtual scroll and column state |
| UI-R2 | WebForms ViewState-dependent code-behind logic has no Vue equivalent | Each code-behind is reviewed during slice migration; logic extracted to API or Pinia action |
| UI-R3 | CKEditor 5 lacks some CKEditor 4 plugins | Plugin gap list documented; acceptable gaps signed off by product owner before migration |
| UI-R4 | Feature flag table adds DB dependency to routing | Flag cache with 60-second TTL; stale-on-error fallback to legacy route |

## Migration Plan

| Week | Activity |
|------|----------|
| 8–9  | Vue 3 SPA scaffold; router, Pinia, Vuetify baseline; Playwright harness |
| 10–11 | Migrate high-traffic read-only views (lists, dashboards) — slice A |
| 12–13 | Migrate form-heavy views (create/edit flows) — slice B |
| 14–15 | Migrate scheduler and calendar views (FullCalendar) — slice C |
| 16–17 | Migrate rich-text editor views (CKEditor 5) — slice D |
| 18   | UAT round with stakeholders; sign-off per slice |
| 19   | All feature flags flipped in staging; prep for Phase 7 |
| 20   | Final UI readiness fixes and cutover support preparation |

## Open Questions

- Q1: Which views are highest business priority for slice ordering? (Product Owner to confirm)
- Q2: Is the CKEditor 5 GPL v2 licence acceptable to legal for the open-source release? (Legal to confirm)

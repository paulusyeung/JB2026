# Phase 2 Vue 3 UI Spike Discovery Report

## Objective
Validate whether the legacy Job.Book OrderList master-detail workflow can be represented with Vue 3 while integrating with a real ASP.NET Core API endpoint.

## Legacy Baseline Used
- Legacy UI reference: `C:/Projects/JB2015/Job.Book/JobOrder/OrderList_MasterDetail.cs`
- Legacy behavior reference: toolbar-driven refresh and master-detail interaction via embedded page.

## Spike Implementation
- UI project: `spikes/phase-2/jb2026-ui-spike`
- Representative screen: master list and detail panel with one-click selection.
- API integration: authenticated calls to `GET /api/v1/jobs/range` and `GET /api/v1/jobs/{id}` from the API pilot.
- Interaction parity covered:
  - load jobs by date range
  - select row to view detail payload
  - surface attachment and style-title detail
  - refresh cycle with role-based demo token generation

## Reusable Patterns Confirmed
- Feature-level state model in a single Vue composition script for bounded screen logic.
- API-first DTO contracts (`JobListItem`, `JobDetail`) shared by typed client code.
- Controlled authentication bootstrap flow: token request followed by authorized API calls.
- Master-detail screen decomposition that maps directly to legacy workflow semantics.

## Known Gaps
- Legacy toolbar command matrix (columns/sorting/multi-select/export) is only partially represented.
- No DevExpress-equivalent chart/grid widget introduced in this spike.
- No route-level auth guard or role-based screen suppression in front-end yet.
- No E2E automation in this spike repo (only compile validation and API integration behavior).

## Effort Estimate for Full Migration
- Foundation and shell parity for similar medium-complex screens: 2-3 days each.
- Full workflow parity with validation/export/print features: 4-7 days each.
- Dependency replacement and cross-screen consistency setup: 1-2 sprints before wide rollout.

## Result
Viable. Vue 3 can represent the selected WebForms/Gizmox master-detail behavior while consuming a modern API contract.
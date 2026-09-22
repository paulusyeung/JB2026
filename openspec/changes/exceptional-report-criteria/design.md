## Context

See proposal.md - Why. `ExceptionalReportView.vue` currently renders every job order returned by `getJobList({ startOn, endOn, take: 500 })` (GET `/api/v2/job-orders?listType=job`). The shared `GetJobList` repo method (EfJobManagementRepository.cs:141) is used by `JobListView`, `DashboardView`, `CrmCustomer360View`, and the exceptional report, so its filter semantics must not change. The EF query already `Include`s `JobSchedules`, but `JobOrderResponse` does not expose schedule data. The view already has the other inputs on `JobOrderRecord` (`orderedOn`, `completedOn`, `invoiceRef`, `originalSONumber` - the field the UI labels COGS). View preferences persist via `useViewSettings` (useColumnPersistence.ts), which currently has a fixed shape.

## Goals / Non-Goals

**Goals:**
- Exceptional report shows only rows matching at least one user-selectable criterion.
- Criteria + day thresholds editable from the view and persisted per user.
- Zero behavior change for the other three consumers of `GetJobList`.
- Threshold changes apply client-side without a server round trip.

**Non-Goals:**
- No server-side filtering of criteria (dataset is capped at 500 rows).
- No "cancelled" criterion (deferred; interpretation unresolved).
- No changes to the shared `GetJobList` filter logic.

## Decisions

### 1. Client-side rule engine in ExceptionalReportView
Represent criteria as a data array - `{ id, days, enabled, test(row, today): boolean }` - that both generates the criteria panel UI and drives a `filteredRows` computed (OR combination). `sortedRows`/pagination/summary chips operate on the filtered set.

**Why:** No shared-API risk; instant response to toggle/threshold changes; 5 predicates on ≤500 rows is negligible. **Alternative considered:** server-computed `exceptionalReasons` flags - rejected because it touches the shared endpoint and forces reloads on every threshold tweak.

### 2. Additive schedule fields on JobOrderResponse
Add `DateTime? ScheduledOn` (earliest schedule with `Cancelled != true`) and `int ActiveScheduleCount` (or a single `HasActiveSchedule` bool) as optional new properties on `JobOrderResponse`, populated in `MapOrder(JobOrder)` (EfJobManagementRepository.cs:726), which already has `job.JobSchedules`. Mirror in `InMemoryJobManagementRepository` for parity tests. The `vwOrderDetailList` mapping is not needed (GetOrderList path is unrelated to this report).

**Why:** Schedule presence is the only input missing from `JobOrderRecord`; the EF query already loads the data, so this is a mapping-only change. **Alternative considered:** new dedicated endpoint `/api/v2/job-orders/exceptional` - rejected as overkill; the additive fields keep a single data source.

### 3. Persist criteria via extendViewSettings
Extend `ViewSettings` in `useColumnPersistence.ts` with an optional `criteria?: Record<string, { enabled: boolean; days: number }>` carried through `parseSettings`/`applySettings`/save with `??` fallback to the view defaults. Backward-compatible: existing view IDs simply lack the key.

**Why:** reuses the established persistence path (localStorage + debounced server preference) instead of inventing a second mechanism. **Alternative considered:** separate localStorage key inside the view - rejected as a parallel, weaker persistence path.

### 4. Criteria panel and per-row reasons
A toolbar popover (consistent with the existing Columns/Sorting menus) with a switch + day-stepper per criterion, plus a "matched reasons" column (`exceptionalReasons`) rendered as chips with i18n labels, toggleable through the existing column picker.

**Why:** fits the view's existing interaction language. Matched-reasons column gives users the "which criteria matched" visibility promised by the spec.

## Risks / Trade-offs

- [Shared model] New `JobOrderResponse` fields are a contract change → additive optional properties; existing consumers ignore them; mirrored in InMemory repo for parity tests.
- [Legacy sentinels] `orderedOn`/`completedOn` may carry 1900-01-01 sentinel values → predicates must treat min/sentinel dates as "absent".
- [Data cap] `take: 500` may truncate a large exceptional backlog → acceptable; matches current report behavior; revisit only if users report truncation.
- [Composable change] Extending `useViewSettings` touches a shared file → change is purely additive and default-fallback; verify existing views' settings still round-trip.

## Migration Plan

1. Backend: add `ScheduledOn`/`HasActiveSchedule` to `JobOrderResponse` + `MapOrder` in EF and InMemory repos.
2. Frontend: extend `ViewSettings` in `useColumnPersistence.ts`.
3. Frontend: add criteria config, predicates, filtering, panel UI, reasons column, i18n keys in `ExceptionalReportView.vue`.
4. Update smoke/component coverage for the report route.
5. Manual check: other views (JobList/Dashboard/CRM360) unaffected; criteria persist across reload.

## Open Questions

- Interpretation of "not scheduled" for schedules that were later cancelled - resolved as `Cancelled != true` only; revisit if a schedule's rescheduled/cancelled semantics differ in practice.
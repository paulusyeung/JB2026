## Why

The Exceptional Report at `/job-order/reports/exceptional` currently just lists every job order in a date range — it never actually filters for "exceptional" cases, even though the name promises a report of exception cases needing attention (anomalies such as jobs never scheduled, never completed, or never invoiced). Users want the report to surface only the exceptional records, with the criteria and thresholds changeable from the view.

## What Changes

- **Client-side rule engine**: The view filters the loaded job orders against a set of toggleable "exceptional" criteria. A row is exceptional when it matches **any** enabled criterion.
- **Five criteria** (each with an editable day threshold, except where noted):
  - Not scheduled within N days of ordering
  - Not completed within N days of ordering
  - No invoice number within N days of ordering
  - No invoice number after completion (no threshold)
  - No COGS (`OriginalSONumber`, the field labeled COGS in the UI) within N days of ordering
- **Criteria UI**: An expandable panel/menu with a toggle and day-threshold input per criterion, persisted per user (same mechanism as column/sorting prefs).
- **Per-row matched reasons**: Show a chips column indicating which criteria a row matched.
- **Small additive backend field(s)**: `JobOrderResponse` gains schedule information (`ScheduledOn` / `HasActiveSchedule`), so the "not scheduled" criterion can be evaluated. The shared `GetJobList` filter behavior is unchanged.
- **No breaking changes**: Other views (`JobListView`, `DashboardView`, `CrmCustomer360View`) are unaffected.

## Capabilities

### New Capabilities
- `exceptional-report-criteria`: selection criteria and thresholds that define which job orders appear in the exceptional report.

### Modified Capabilities
- `exceptional-job-orders-report`: the report now filters rows by selectable exceptional criteria (toggle + day threshold) instead of showing all job orders in the date range.

## Impact

- **Backend**: `JobOrderResponse` model gains additive schedule fields; `MapOrder` in `EfJobManagementRepository` and `InMemoryJobManagementRepository` populate them (EF query already loads `JobSchedules`). No shared filter logic changes.
- **Frontend**: `ExceptionalReportView.vue` gains the criteria state, rule predicates, filtering computed, criteria panel UI, matched-reasons column, and i18n keys (`reports.exceptional.criteria.*`). `useViewSettings` persistence key extended.
- **Tests**: Component/smoke coverage for the exceptional report route; verify existing smoke test still passes (view renders).
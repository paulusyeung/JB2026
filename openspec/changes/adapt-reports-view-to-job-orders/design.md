## Context

Two report views exist:
- `ReportsView.vue` at `/reports` — simple, quotation-based, orphan route (not in sidebar)
- `ExceptionalReportView.vue` at `/job-order/reports/exceptional` — rich, job-order-based, in sidebar

Both conceptually do "exceptional report" but against different domains. The business value is in job orders. The quotation view is dead weight.

## Goals / Non-Goals

**Goals:**
- Single exceptional report view showing job orders
- Date range picker (replacing month picker)
- Summary chips (total rows, total amount) absorbed from `ReportsView`
- Remove all dead code from the old quotation-based report path

**Non-Goals:**
- No backend changes — `ExceptionalReportView` already uses `GET /api/v2/job-orders?listType=job` via `getJobList()`
- Not making reports fully generic (future enhancement)
- The `ReportsController` and its models are removed, not refactored

## Decisions

### 1. Merge into ExceptionalReportView, remove ReportsView
`ExceptionalReportView.vue` is the richer component (column picker, sorting, card/detail toggle, checkbox mode, editor dialog, print manager, invoice hydration). `ReportsView.vue` contributes: date range picker UX and summary chips. These are absorbed into `ExceptionalReportView`.

### 2. Date range picker replaces month picker
Replace the `<input type="month">` with start date + end date `<input type="date">` fields. The `getJobList()` call already supports `startOn`/`endOn` parameters — no API change needed.

### 3. Remove the entire old reports path
- `ReportsController.cs` — delete
- `RunReportRequest.cs`, `ReportRunResponse.cs`, `QuotationListItemResponse.cs` — delete if orphaned
- `services/reports.ts` — delete
- `/reports` route — redirect to `/job-order/reports/exceptional` or remove

### 4. Route stays at /job-order/reports/exceptional
Future report types can be added as `/job-order/reports/<type>`.

## Risks / Trade-offs

- [Low] Removing `ReportsController` / models: verify nothing else references these types (grep before deleting).
- [Low] The `/reports` route: check for any external links or bookmarks.
- [Clean win] Less code, clearer domain model, no confusing dual-reports state.

## Migration Plan

1. Update `ExceptionalReportView.vue`: add date range picker, add summary chips, update i18n
2. Remove `ReportsView.vue`, `services/reports.ts`, `/reports` route (redirect or delete)
3. Remove backend: `ReportsController.cs`, `RunReportRequest.cs`, `ReportRunResponse.cs` (if orphaned)
4. Verify the view works end-to-end

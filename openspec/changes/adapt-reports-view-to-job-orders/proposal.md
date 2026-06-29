## Why

The `ReportsView.vue` at `/reports` is a simple view that queries quotations via `POST /api/v2/reports/run` but was never linked from the sidebar. Meanwhile `ExceptionalReportView.vue` at `/job-order/reports/exceptional` is a full-featured job order list view with column picker, sorting, card/detail toggles, editor dialog, print manager, and invoice hydration. Both are conceptually "exceptional reports" but against different domains (quotations vs job orders). Job orders are the core domain — the quotation-based view adds confusion and dead code.

## What Changes

- **Merge**: Absorb `ReportsView.vue` into `ExceptionalReportView.vue`. The richer component survives, the simpler one is removed.
- **Data source**: `ExceptionalReportView.vue` already uses `GET /api/v2/job-orders?listType=job` — no backend changes needed.
- **Date picker**: Replace month picker with a date range picker (start date + end date).
- **Cleanup**: Remove `ReportsView.vue`, `ReportsController.cs`, `RunReportRequest.cs`, `ReportRunResponse.cs`, `services/reports.ts`, and the orphan `/reports` route.
- **Route stays**: The merged view lives at `/job-order/reports/exceptional`, keeping the door open for more report types under this tree.

## Capabilities

### Modified Capabilities
- `exceptional-job-orders-report` (spec): Updated to remove quotation-specific scenarios, reflect the single merged view with job order data.

## Impact

- **Backend**: Remove `ReportsController.cs`, `RunReportRequest.cs`, `ReportRunResponse.cs`, `QuotationListItemResponse.cs` (if orphaned).
- **Frontend**: Remove `ReportsView.vue`, `services/reports.ts`, the `/reports` route. Update `ExceptionalReportView.vue` to add date range picker and summary chips from `ReportsView`. Update i18n keys.
- **Tests**: Remove parity tests referencing the old reports endpoint if any exist.

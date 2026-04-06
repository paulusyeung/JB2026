## Why

Invoice Stats in SML currently uses a custom-rendered table, which does not faithfully replicate the legacy OLAP interaction model and makes maintenance harder. We need a dedicated OLAP grid implementation now to preserve user workflow parity during migration and reduce custom pivot logic risk.

## What Changes

- Replace the Invoice Stats custom pivot table UI with the WebPivotTable OLAP grid component in ClientApp.
- Keep the existing Invoice Stats data source contract and filters (date range, lookup, take) while adapting data mapping for OLAP-grid input.
- Configure the default OLAP layout to match legacy behavior: row dimensions, column dimensions, measure, totals, and paging/export expectations.
- Add resilient client integration behavior for loading, empty/error states, and safe fallback when OLAP init fails.
- Add verification coverage for layout/data mapping and legacy parity-critical totals.

## Capabilities

### New Capabilities
- `sml-invoice-stats-olap-grid`: Provide a legacy-parity OLAP grid experience for Invoice Stats using WebPivotTable, including default layout and backend-driven dataset mapping.

### Modified Capabilities
- None.

## Impact

- Affected frontend view and service usage in SML Invoice Stats flow.
- New frontend dependency: WebPivotTable package installation and bundler compatibility handling.
- No API contract expansion required; existing invoice-stats endpoint remains the source of truth.
- Testing impact on ClientApp unit/integration tests and parity validation workflow for Invoice Stats totals.

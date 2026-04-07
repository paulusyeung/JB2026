## Why

Invoice Stats in SML currently uses a custom-rendered table, which does not faithfully replicate the legacy OLAP interaction model and makes maintenance harder. We need a dedicated OLAP grid implementation now to preserve user workflow parity during migration and reduce custom pivot logic risk.

## What Changes

- Replace the Invoice Stats custom pivot table UI with the WebPivotTable OLAP grid component in ClientApp.
- Keep the existing Invoice Stats data source contract and filters (date range, lookup) while adapting data mapping for OLAP-grid input.
- Configure the default OLAP layout to match legacy behavior: row dimensions, column dimensions, measure, totals, and paging/export expectations.
- Add resilient client integration behavior for loading, empty/error states, and safe fallback when OLAP init fails.
- Add verification coverage for layout/data mapping and legacy parity-critical totals.

## Implementation Learnings to Preserve

- Use the WebPivotTable tabular initialization API with the two-argument signature (`attrArray`, `dataArray`) and not a single object-array payload, otherwise the grid can appear loaded but fail with invalid-data behavior.
- Treat OLAP hydration as asynchronous and wait for custom-element readiness (`customElements.whenDefined`) plus method availability before configuring pivot layout.
- Keep retry-based hydration guards for slow element initialization paths to avoid intermittent blank-grid render states.
- Force the web component host to block layout with explicit height/min-height; default inline custom-element layout can clip rendered content and look empty.
- Initialize the grid in `displayMode: 'grid'` for Invoice Stats parity rather than relying on library defaults.
- Avoid default hard row caps during Invoice Stats fetch/hydration so the OLAP view can load full datasets used in parity checks.
- Format Amount as currency-style numeric output with thousands separators and two decimals for operator readability and parity with reporting expectations.

## Capabilities

### New Capabilities
- `sml-invoice-stats-olap-grid`: Provide a legacy-parity OLAP grid experience for Invoice Stats using WebPivotTable, including default layout and backend-driven dataset mapping.

### Modified Capabilities
- None.

## Impact

- Affected frontend view and service usage in SML Invoice Stats flow.
- New frontend dependency: WebPivotTable package installation and bundler compatibility handling.
- No API contract expansion required; existing invoice-stats endpoint remains the source of truth, with optional row limiting only when explicitly requested.
- Testing impact on ClientApp unit/integration tests and parity validation workflow for Invoice Stats totals.

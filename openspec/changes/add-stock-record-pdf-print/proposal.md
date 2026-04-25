## Why

The stock record dialog exposes a Print action, but it does not produce the operational PDF currently used by users. This gap blocks parity with the legacy workflow and prevents users from generating the movement-history report directly from the modern UI.

## What Changes

- Add a backend print endpoint that generates a stock record PDF from current product data and movement history.
- Connect the Product Record dialog Print button to request and download/open the generated PDF.
- Introduce a print data mapping layer to match the legacy report structure (header fields, remarks, MQ/balance summary, and numbered movement rows).
- Add parity-focused tests for report content ordering, totals/balance values, and date/time formatting to reduce regressions.
- Add user-facing error handling for failed print generation and service-level logging for troubleshooting.

## Capabilities

### New Capabilities
- `stock-record-print-pdf`: Generate and deliver a stock record movement report PDF from the Product Record dialog with legacy-compatible structure.

### Modified Capabilities
- None.

## Impact

- Affected frontend: `JB2026.WebApp/ClientApp/src/components/stock/ProductRecordDialog.vue`, stock service client methods, i18n message keys for print errors/status.
- Affected backend: `JB2026.Api` stock controller/service/reporting layer, optional PDF rendering utility integration, DI registration.
- Affected tests: API/report parity tests in `JB2026.Api.ParityTests`, plus frontend integration/unit coverage for print action behavior.
- Operational impact: introduces a PDF generation path and binary response handling; requires confirmation of font support for CJK content and environment compatibility.

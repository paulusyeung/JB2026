## Why

The modern job-order screens do not reproduce the legacy print workflow. List-level Print actions still open the browser print dialog, and form-level Print Order jumps straight to a default PDF, so users cannot choose layout, suppress pictures or product details, or limit the output to selected workflows before generating the report.

## What Changes

- Add a modern job-order print manager dialog that mirrors the legacy PrintManager flow before PDF generation.
- Route the Print actions from JobListView and the shared JobOrderForm to the same print manager workflow instead of using `window.print()` or a fixed PDF request.
- Add a parameterized backend print contract for job orders so the selected layout, workflow subset, and output toggles drive PDF generation.
- Implement the job-order PDF path on the same backend QuestPDF reporting engine already used for stock record printing, matching the legacy JobOrder report structure including workflow sections, optional product details, and optional attachment images.
- Add parity-focused tests for print options, PDF response behavior, and representative content/layout decisions needed for legacy validation.

## Capabilities

### New Capabilities
- `job-order-print-manager`: Configure and generate legacy-compatible job-order PDFs from modern job-order screens.

### Modified Capabilities

## Impact

- Affected frontend: `JB2026.WebApp/ClientApp/src/views/JobListView.vue`, `JB2026.WebApp/ClientApp/src/components/forms/JobOrderForm.vue`, shared job-print service client methods, and any other views using the shared `print-order` event path.
- Affected backend: job-order print controller/endpoint surface, print request models, job/workflow data composition services, and the same QuestPDF report document/rendering infrastructure currently used by stock record printing in `JB2026.Api`.
- Affected tests: API parity/integration tests for print responses and option handling, plus frontend coverage for dialog-driven print initiation and error handling.
- Operational impact: introduces a richer PDF generation path for job orders, requires CJK-safe font handling and representative legacy PDF comparison during rollout.
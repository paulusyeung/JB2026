## Context

The legacy application opens a dedicated PrintManager dialog before printing a job order. That dialog lets users confirm the order number, choose a layout, suppress pictures, suppress product details, and limit the report to selected workflows. When users click Print Order, the legacy code routes those selections into overloaded `Utility.JobOrder.PrintOrder(...)` entry points and generates a report using `JobOrderXr` or `JobOrderXr2` depending on the selected content options.

The current JB2026 frontend does not reproduce that workflow. In `JobListView.vue`, the Print action still calls the browser print dialog. In the shared `JobOrderForm.vue`, Print Order emits directly to a blob download/open path with no option dialog. The API already uses QuestPDF for stock-record printing through the existing backend reporting pipeline, so the job-order print flow will reuse that same reporting foundation rather than reintroducing legacy report technology.

## Goals / Non-Goals

**Goals:**
- Provide a single print-manager workflow that can be launched from JobListView and the shared JobOrderForm.
- Preserve the legacy option set that materially changes output: layout selection, no picture, no product details, and selected workflows.
- Generate a PDF from the backend using explicit print options rather than browser-page printing.
- Keep the report path testable through a typed request model, deterministic data composition, and parity-oriented PDF validation.

**Non-Goals:**
- Recreate every legacy report type exposed by the old layout combo if the modern product does not yet support those formats.
- Rebuild the legacy WinForms/VWG UI exactly; only the workflow and options need parity.
- Implement bulk printing across multiple job orders in this change.
- Migrate unrelated list views that expose generic browser print actions unless they use the same job-order workflow.

## Decisions

1. Add a shared frontend print-manager dialog for job-order printing.
- Decision: introduce a dedicated Vue dialog component that receives the active job/order context, loads available workflows, captures print options, and returns a print request payload.
- Rationale: keeps print behavior consistent across JobListView and JobOrderForm, avoids duplicating form state logic, and matches the legacy mental model shown in the screenshots.
- Alternative considered: add a few inline checkboxes directly beside each existing Print button. Rejected because the legacy workflow is modal, includes workflow selection, and is reused from multiple entry points.

2. Replace direct browser printing and fixed PDF fetches with a parameterized API request.
- Decision: add a dedicated job-order print endpoint that accepts order ID plus option fields such as layout, no-picture, no-product-details, and selected workflow indices, then returns `application/pdf`.
- Rationale: the browser print dialog cannot express legacy print options, and the current fixed blob fetch cannot represent output variants.
- Alternative considered: continue using the existing PDF endpoint and encode defaults only in the UI. Rejected because it preserves the main parity gap.

3. Compose job-order report data in a server-side print model before rendering.
- Decision: create a report-composition layer that maps job details, workflow metadata, product details, and attachment previews into a print document model consumed by QuestPDF.
- Rationale: isolates parity logic from controllers, makes option handling testable, and supports future layouts without leaking formatting rules into transport code.
- Alternative considered: render directly from EF or DTO objects in the controller. Rejected because it couples data access, formatting, and transport in one place.

4. Implement the job-order report using the same backend QuestPDF engine used by stock record printing and model explicit content toggles.
- Decision: build one or more QuestPDF document compositions on the existing backend reporting foundation so job-order printing follows the same server-side PDF generation approach as stock-record printing, including the alternate rendering path when product details are suppressed and the optional attachment image block.
- Rationale: QuestPDF is already present in `JB2026.Api` and already powers stock-record PDF generation, so reusing that engine keeps report infrastructure consistent and avoids introducing a second reporting engine.
- Alternative considered: keep the legacy DevExpress/XtraReport template semantics or generate HTML for browser printing. Rejected because the modern stack already standardized on server-side PDF generation.

5. Scope parity validation around observable output decisions, not byte-for-byte PDF identity.
- Decision: verify endpoint behavior, selected-option propagation, required content presence/absence, workflow filtering, and representative CJK text extraction rather than requiring binary-equal PDFs.
- Rationale: QuestPDF output will not be byte-identical to the legacy renderer, but users need behavioral and visual parity.
- Alternative considered: byte compare against legacy PDFs. Rejected because renderer internals, timestamps, and object ordering make that brittle.

## Risks / Trade-offs

- [Legacy layout semantics are broader than the first modern implementation] -> Mitigation: document which layouts are supported in this change and preserve extension points for additional print forms.
- [Workflow filtering may not match legacy indexing or ordering] -> Mitigation: derive workflow selections from the same ordered workflow list used in the dialog and add parity tests for selected subsets.
- [Attachment previews or PDF-derived thumbnails may render inconsistently] -> Mitigation: define supported image-selection rules and test both image and PDF-preview attachment cases.
- [CJK text or mixed-language workflow content renders incorrectly] -> Mitigation: reuse the established QuestPDF font configuration and validate with multilingual fixtures.
- [Shared JobOrderForm usage broadens impact beyond JobListView] -> Mitigation: centralize the new print dialog service and regression-test every view that consumes the shared print event.

## Migration Plan

1. Introduce the new print request contract and backend PDF composition behind a new or versioned endpoint while keeping the existing fixed PDF path available during development.
2. Wire the shared print-manager dialog into JobOrderForm and JobListView, then update other consumers of the shared `print-order` event to use the same flow.
3. Validate the generated PDF against representative legacy samples for the default job-order layout and option combinations.
4. Remove or stop using the obsolete browser `window.print()` path for job-order printing after parity sign-off.

## Open Questions

- Which legacy `PrintFormType` values must be supported in the first JB2026 release, beyond the default Job Order layout shown in the screenshots?
- Should product-detail section selection and supplier-specific purchase-order behavior remain out of scope for this first job-order print-manager cut, or should the dialog reserve UI/API fields for them now?
- Should the existing `/api/Job/pdf/{id}` endpoint be replaced outright, or retained as a compatibility shortcut that maps to default print-manager options?
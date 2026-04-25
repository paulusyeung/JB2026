## Context

The Product Record dialog already has a Print button, but it is currently gated and does not execute report generation. Legacy users rely on a PDF stock report that includes product identity details, remarks, MQ/balance summary, and a numbered movement-history table sorted by movement date/time. The migration requires functional parity so users can continue print/export workflows without leaving the modern UI.

The implementation spans frontend (button action, binary download/open, error UX), API (report endpoint), application/service mapping (report data shaping), and PDF rendering infrastructure (font-safe output for multilingual text). Existing movement APIs and product detail retrieval can be reused as source data.

## Goals / Non-Goals

**Goals:**
- Provide a working Print action in Product Record dialog that returns a downloadable/openable PDF report.
- Produce report output with legacy-compatible structure and ordering so users can verify parity visually and operationally.
- Keep print behavior deterministic via explicit formatting rules (date/time display, row numbering, quantity signs, running balance values).
- Add automated tests that verify core report content and protect parity during future refactors.

**Non-Goals:**
- Rebuild all legacy report templates or add a generic report designer.
- Implement batch/multi-product printing in this change.
- Introduce asynchronous print-job queues unless required by production load.
- Change stock movement business logic or inventory computation rules.

## Decisions

1. Add a dedicated API endpoint for stock record PDF output.
- Decision: Introduce a focused endpoint (for example: `GET /api/stock/products/{productId}/print`) returning `application/pdf`.
- Rationale: Keeps print concerns explicit, avoids overloading existing JSON endpoints, and supports browser-native file handling.
- Alternative considered: generating PDF in frontend from JSON. Rejected due to layout parity complexity and font/rendering inconsistency.

2. Build a report view model that maps domain data into print-ready sections.
- Decision: Add a server-side mapper that composes header block, summary block, and table rows from product + movement data.
- Rationale: Centralizes formatting and parity logic, making tests stable and independent of controller wiring.
- Alternative considered: map directly in controller. Rejected to avoid duplicated formatting rules and poor testability.

3. Use deterministic movement ordering and row numbering in the PDF pipeline.
- Decision: Sort movements by in/out datetime descending, then modified datetime descending; assign row numbers after sorting.
- Rationale: Matches current dialog history ordering and avoids ambiguities when timestamps collide.
- Alternative considered: preserve DB/default order. Rejected because it can vary between environments.

4. Implement PDF rendering behind an interface and choose a font-capable renderer.
- Decision: Introduce an abstraction (for example `IStockReportPdfRenderer`) and use a renderer/package that embeds or references fonts supporting CJK text.
- Rationale: Supports future renderer swaps, improves testability through mockable contracts, and addresses multilingual report data seen in legacy output.
- Alternative considered: hardwire renderer calls in service. Rejected for tight coupling and harder tests.

5. Handle print invocation on frontend as binary response flow.
- Decision: Replace gated Print action with service call that requests blob data, opens in new tab where allowed, and falls back to file download.
- Rationale: Consistent UX in SPA environments and compatible with browser popup constraints.
- Alternative considered: navigate away to print route. Rejected because it disrupts dialog context.

## Risks / Trade-offs

- [PDF layout drifts from legacy template] -> Mitigation: parity checks on representative fixtures and explicit acceptance criteria in specs.
- [CJK glyphs render as tofu/missing characters] -> Mitigation: configure font embedding/fallback and validate with multilingual test data.
- [Large movement history increases render time] -> Mitigation: cap page render memory, optimize table pagination/streaming if supported by renderer, measure in parity tests.
- [Browser popup blockers prevent inline open] -> Mitigation: include deterministic download fallback and user-visible message when inline open fails.
- [Binary response mishandling in client] -> Mitigation: typed API client method returning Blob and integration test around response headers/content-type.

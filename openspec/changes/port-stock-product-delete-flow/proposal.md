# Proposal - port-stock-product-delete-flow

## Why

Stock product delete is still unavailable in the modern ClientApp, leaving the `Delete` actions in StockView and ProductRecordDialog as dead-end placeholders. This blocks parity with JB2015 and forces users to return to legacy screens for a core maintenance workflow.

## What Changes

- Implement stock product delete behavior in ClientApp for two entry points:
  - Stock list toolbar action in `StockView`.
  - Record-level action in `ProductRecordDialog`.
- Port legacy lifecycle semantics from JB2015 `Utility.Product.Delete(Guid productId)`:
  - First delete request performs soft-delete (mark record retired with audit metadata).
  - Delete on already-retired record performs hard delete.
- Port hard-delete cleanup behavior to keep data/files consistent:
  - Remove related stock in/out rows.
  - Remove related product attachment rows.
  - Remove physical attachment/image files, then related attachment metadata.
- Support checkbox-selection delete from stock list with parity-aligned confirmation UX for single and batch context.
- Surface clear success/failure feedback and refresh list/dialog states after deletion so visible data reflects current lifecycle state.
- Add or extend backend endpoints/service methods and validations to enforce lifecycle and cleanup rules server-side.

## Capabilities

### New Capabilities
- `stock-product-delete-lifecycle`: Enable parity-aligned stock product delete across list and record dialogs with two-step retire/hard-delete behavior and cascading cleanup.

### Modified Capabilities
- None.

## Impact

- Affected UI modules:
  - `JB2026.WebApp/ClientApp/src/views/StockView.vue`
  - `JB2026.WebApp/ClientApp/src/components/stock/ProductRecordDialog.vue`
  - Stock service/composables used by both views.
- Affected backend/data modules:
  - Product delete API route(s) and domain service(s) for soft-delete vs hard-delete branching.
  - Attachment and stock movement cleanup paths and file deletion orchestration.
  - Audit metadata writes for retire operations.
- Affected testing:
  - Frontend behavior tests for confirmation, single/batch delete, and refresh/update states.
  - API parity/correctness tests for retire first-pass, hard delete second-pass, and cleanup integrity.
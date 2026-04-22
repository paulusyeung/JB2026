# Proposal - port-stock-in-out-transaction-popup

## Why

Stock Product parity is currently incomplete because users cannot register stock movement transactions from the modern UI, even though the legacy workflow depends on a dedicated Stock In/Out popup. This gap prevents end-to-end stock operations in ClientApp and forces users back to JB2015 for a core daily task.

## What Changes

- Add a Stock In/Out transaction dialog in ClientApp that mirrors the legacy StockInOut form behavior.
- Open this dialog from two entry points:
  - `StockView` toolbar Stock In/Out action when a row is selected.
  - `ProductRecordDialog` Stock In/Out action for the active product.
- Implement form fields and defaults matching legacy semantics:
  - Stock Number (pre-filled/read-only from selected product context)
  - Date (default to today, editable)
  - Reference (optional text)
  - Quantity (+/-) integer (required; positive for stock-in, negative for stock-out)
- Implement legacy-equivalent validation and save flow:
  - Stock number must resolve to an existing product.
  - Quantity must be non-empty numeric integer.
  - Save confirmation before commit.
  - Save and Close as the primary action.
- Persist transaction and update product balance atomically via API.
- Refresh stock-related views after successful save so balance and movement history remain consistent.

## Capabilities

### New Capabilities
- `stock-in-out-transaction-entry`: Enable users to create stock movement transactions from stock list and product record contexts with parity-aligned validation, confirmation, and persistence behavior.

### Modified Capabilities
- None.

## Impact

- Affected UI modules:
  - `JB2026.WebApp/ClientApp/src/views/StockView.vue`
  - `JB2026.WebApp/ClientApp/src/components/stock/ProductRecordDialog.vue`
  - New Stock In/Out dialog component and related composables/services.
- Affected API/domain surfaces:
  - Stock/product service endpoints for creating in/out transactions and updating balance.
  - Movement-history and list refresh paths to show immediately consistent balances.
- Affected testing:
  - Vue component tests for dialog triggering, validation, and save flow.
  - API parity/correctness tests for transaction persistence and balance updates.

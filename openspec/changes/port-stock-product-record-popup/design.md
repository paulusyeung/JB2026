# Design - port-stock-product-record-popup

## Context

StockView currently provides list/search/sort/export capabilities but no create/edit dialog. Legacy ProductRecord behavior includes a modal form with required field validation, save/delete actions, and stock movement history for existing products. This design ports that behavior into Vue 3 + Vuetify while preserving current architecture patterns in ClientApp.

## Goals

- Add a single reusable dialog for both create and edit product record flows.
- Trigger edit from stock row click and create from NEW PRODUCT.
- Preserve key legacy validation and mode-specific behavior.
- Keep implementation incremental so non-ported actions remain explicitly gated.

## Non-Goals

- Implement attachment manager in this change
- Implement stock in/out entry subflow in this change
- Implement reporting print/export in this change

## UI Architecture

### Components

- StockView integration updates:
  - Row click handler opens dialog in edit mode with selected product id.
  - NEW PRODUCT button opens dialog in create mode.
- New ProductRecordDialog component:
  - Props: open, mode (create/edit), productId (optional)
  - Emits: saved, deleted, close
  - Internal sections:
    - Identity fields and stock number composer
    - Product data fields
    - Action toolbar (Save, Save and Close, Delete, gated actions)
    - Movement history table (edit mode only)

### State Model

- Dialog state in StockView:
  - dialogOpen: boolean
  - dialogMode: create | edit
  - activeProductId: string | null
- Dialog local state:
  - form model
  - validation errors
  - loading/saving/deleting flags
  - movement history rows
  - productCodeChanged marker for uniqueness checks in edit mode

## Data Contracts

### Service Layer Additions

Add stock product detail service methods (or map to existing endpoints if available):

- getProductRecord(productId)
- createProductRecord(payload)
- updateProductRecord(productId, payload)
- deleteProductRecord(productId)
- getProductStockMovements(productId)
- getNextProductNumber()
- validateProductCodeUniqueness(productCode, excludeProductId?)

### Mapping Rules

- Compose stock number as customerCode + categoryCode + padded number.
- Parse composed stock number into segments in edit mode for display.
- Balance remains derived/read-only in form.
- Movement history balance column is running balance by movement order.

## Validation and Behavior Parity

- Required fields: customerCode, categoryCode, number, productCode, productName.
- Product code uniqueness checks:
  - Always in create mode.
  - In edit mode only if productCode changed.
- Confirmations:
  - Save
  - Save and Close
  - Delete
- Save transition:
  - If current mode is create and save succeeds, switch to edit mode for the created record.

## Interaction Flows

1. Edit flow:
   - User clicks a row in stock table/card.
   - Dialog opens in edit mode and loads product + movement history.
   - User edits values, saves, StockView refreshes list and remains open or closes based on action.

2. Create flow:
   - User clicks NEW PRODUCT.
   - Dialog opens with blank defaults.
   - User requests next number, enters required fields, saves.
   - System creates record, refreshes StockView list, and transitions dialog to edit mode.

3. Delete flow:
   - Available only in edit mode.
   - Confirm delete, call delete API, close dialog on success, refresh StockView.

## Testing Strategy

- Unit/component tests:
  - Dialog validation matrix for required fields and uniqueness checks
  - Mode transition create -> edit on save
  - Row click and new product open behavior in StockView
- API integration/parity tests:
  - Create/update/delete product correctness
  - Product code uniqueness parity behavior
  - Movement history projection and formatting
- Manual QA:
  - Desktop and mobile dialog behavior
  - Error handling, confirmations, and loading states

## Risks and Mitigations

- Risk: missing API endpoints for detail CRUD and movements.
  - Mitigation: define adapter interface first and stub against existing stock APIs; align with backend migration tasks.
- Risk: parity gaps in stock number generation.
  - Mitigation: centralize number composition/parsing and add tests for edge cases.
- Risk: user confusion from partially gated actions.
  - Mitigation: explicit labels and tooltips indicating phased availability.

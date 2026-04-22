# Proposal - port-stock-product-record-popup

## Why

The current stock page lists products but does not provide the legacy create/edit workflow where users open a product record form, update details, and review stock movement history in context. This blocks parity for a core operational flow and forces users to remain in the legacy system for daily product maintenance.

## What Changes

Add a modal-based Product Record flow in the ClientApp stock module:

- Clicking a row in the stock list opens a product record dialog in edit mode.
- Clicking NEW PRODUCT opens the same dialog in create mode with blank fields.
- The dialog layout and behavior will mirror legacy ProductRecord UX and rules where practical in Vuetify.
- Save, Save and Close, and Delete behaviors are implemented with confirmations and validation.
- Edit mode includes stock movement history (date, reference, qty, running balance, modified on/by).
- Existing actions that are not yet ported (Attachment, Stock In/Out, Print, Export) are shown as gated actions with explicit phase notes.

## Functional Scope (Parity Targets)

- Header and identity fields:
  - Stock Number components (customer code, category code, sequence number)
  - Product Code (required, uniqueness check)
  - Product Name (required)
- Content fields:
  - Production Info / Description
  - Remarks
  - Selling Price
  - COGS
  - Balance (read-only)
- Mode behaviors:
  - Create mode starts blank and can request next stock number
  - Edit mode loads existing product and movement history
  - Post-save from create transitions dialog to edit mode for the newly created record
- Validation:
  - Required checks for customer code, category code, stock number, product code, product name
  - Product code uniqueness check on create and when changed during edit

## Impact

- Users can create and maintain products directly in ClientApp without leaving the modern UI.
- Stock module parity increases for Phase 6 migration and reduces dual-entry risk.
- Requires stock product detail CRUD API support if not already present.
- Requires focused parity tests for create/edit and validation rules.

## Non-Goals

- Full migration of attachment management UX in this slice
- Full migration of stock in/out transaction entry workflow in this slice
- Pixel-perfect replication of legacy WebForms control styling
- Reworking stock domain rules beyond legacy parity

## 1. Backend Invoice Editor Contracts

- [ ] 1.1 Define normalized billing invoice editor request/response DTOs for invoice detail, create, update, and Invoice Ninja client selection.
- [ ] 1.2 Add `GET /api/v2/billing/clients` endpoint for Invoice Ninja-backed client lookup/search.
- [ ] 1.3 Add `GET /api/v2/billing/invoices/{externalInvoiceId}` endpoint returning normalized invoice editor detail.
- [ ] 1.4 Add `POST /api/v2/billing/invoices` endpoint that creates a new Invoice Ninja invoice from the normalized payload.
- [ ] 1.5 Add `PUT /api/v2/billing/invoices/{externalInvoiceId}` endpoint that updates an existing invoice only when it is still `Draft`.
- [ ] 1.6 Ensure backend validation rejects non-draft updates, missing client selection, invalid dates, and malformed line items with stable billing error responses.

## 2. Backend Invoice Ninja Mapping

- [ ] 2.1 Implement Invoice Ninja client option retrieval/mapping for the dialog picker.
- [ ] 2.2 Implement normalized invoice-detail mapping from Invoice Ninja invoice payloads into the dialog DTO.
- [ ] 2.3 Implement create-invoice mapping for client, invoice date, job number, and manual line items including `P.O.Number` and `unit` field handling.
- [ ] 2.4 Implement draft-only update mapping for the same normalized payload.
- [ ] 2.5 Recompute line and total amounts on the backend before persisting to Invoice Ninja.

## 3. Frontend Billing Services

- [ ] 3.1 Extend `ClientApp/src/services/billing.ts` with types and functions for client lookup, invoice detail load, invoice create, and invoice update.
- [ ] 3.2 Normalize backend validation and transport errors into UI-friendly messages.
- [ ] 3.3 Add service-level tests if the current billing test structure supports them.

## 4. Shared Invoice Dialog UI

- [ ] 4.1 Add a dedicated billing invoice dialog component/composable for `create`, `edit`, and `view` modes.
- [ ] 4.2 Implement client selector, invoice date input, job number input, and editable line-item table.
- [ ] 4.3 Support add/remove line items and compute per-line totals plus the overall invoice total live in the UI.
- [ ] 4.4 Disable all editable controls in `view` mode for non-draft invoices.
- [ ] 4.5 Add save/cancel actions and pending-state handling.
- [ ] 4.6 Surface validation and save errors in localized UI.

## 5. Billing Invoices View Integration

- [ ] 5.1 Replace `openNewInvoice()` navigation with dialog open in `create` mode.
- [ ] 5.2 Replace invoice-number click navigation with dialog open plus detail fetch.
- [ ] 5.3 Derive dialog mode from authoritative invoice status (`Draft` => edit, otherwise view).
- [ ] 5.4 Refresh or patch the billing list after successful create/update so the toolbar and list remain in sync.
- [ ] 5.5 Preserve existing `Mark Sent` and `Download` actions alongside the new dialog behavior.

## 6. Localization

- [ ] 6.1 Add i18n keys for dialog titles, fields, line-item headers, totals, buttons, and validation messages.
- [ ] 6.2 Update all supported billing locale files consistently.

## 7. Verification

- [ ] 7.1 Add backend tests for create, draft-only update, detail mapping, and client lookup.
- [ ] 7.2 Add frontend/component tests for dialog mode switching, read-only gating, line-total math, and save behavior.
- [ ] 7.3 Run targeted API and web-app validation for the new billing invoice dialog flow.
## Why

`BillingInvoicesView` currently stops at list-level actions. Users can create invoices only through indirect job-order flows, and once invoices exist in the billing list there is no in-app way to open a structured editor to create a manual invoice, revise a draft invoice, or inspect a sent/paid invoice without leaving JB2026 for Invoice Ninja.

That gap breaks the billing workflow at the point where users already manage invoice summaries. The billing list should become the operational entry point for invoice authoring and review while still keeping Invoice Ninja as the source of truth and keeping all Invoice Ninja credentials on the backend.

## What Changes

- Add a shared billing invoice dialog that opens from `BillingInvoicesView`.
- Change the `New Invoice` toolbar action so it opens the dialog in create mode instead of navigating away.
- Change invoice-number click behavior so it opens the same dialog in edit mode for `Draft` invoices and read-only view mode for non-draft invoices.
- Allow the dialog to capture Invoice Ninja-backed invoice inputs: client selection, invoice date, job number, and an editable multi-line item grid with `P.O.Number`, description, qty, unit, unit cost, calculated line total, and overall invoice total.
- Fetch selectable clients from Invoice Ninja through a backend proxy endpoint.
- Save new and draft-edited invoices through backend billing endpoints that create or update the Invoice Ninja invoice directly; do not persist draft form state in `ClientApp`.
- Make all new labels, validation messages, button text, and dialog states i18n-ready.

## Capabilities

### New Capabilities
- `billing-invoice-editor-dialog`: Shared create/edit/view dialog for billing invoices with line-item authoring and total calculation.

### Modified Capabilities
- `billing-invoice-list-ui`: The billing list toolbar and invoice-number interaction open the invoice editor dialog.
- `invoice-lifecycle-mgmt`: Manual invoice create and draft-only update flows are added to the existing Invoice Ninja lifecycle contract.

## Impact

- Frontend UI: `JB2026.WebApp/ClientApp/src/views/BillingInvoicesView.vue` and a new/reused dialog component for invoice create/edit/view behavior.
- Frontend services: billing service types and API methods for listing Invoice Ninja clients, fetching invoice detail, creating invoices, and updating draft invoices.
- Backend/API: billing endpoints in `JB2026.Api` to proxy Invoice Ninja client lookup, invoice detail retrieval, invoice creation, and draft-only invoice updates.
- Invoice Ninja integration: expands the current backend-owned contract from summary/read/send/download operations into structured authoring and editing.
- Localization: new `billing.invoices.*` dialog and validation keys across supported locale files.
- Testing: focused coverage for dialog mode selection, read-only gating, line-total calculations, validation, backend draft-only updates, and list refresh after save.

## Design Considerations & Accessibility

[!NOTE]
The dialog should trap focus, provide keyboard navigation for the line‑item grid, and expose ARIA labels for all actionable controls.

**UX refinements**
- Distinguish **Create**, **Edit**, and **Read‑Only** modes with a header badge and disable inputs in view mode.
- Show inline validation errors and a summary alert for accessibility.
- Use a debounced search for the client selector and lazy‑load results to keep the UI responsive with large client lists.
- Provide secondary actions (PDF/Download/Send) in the dialog footer for non‑draft invoices only if agreed upon.

**DTO shape**
- Introduce a normalized `InvoiceEditorDto` returned by the backend; it abstracts Invoice Ninja’s payload and isolates UI from API changes.

**Concurrency handling**
- Backend should return `409 Conflict` when a draft is sent elsewhere; the UI will surface a modal “Invoice state changed – refresh and try again”.

**Numeric validation**
- Qty must be > 0, unit cost ≥ 0, both with two‑decimal precision. Rounding follows banker's rounding.

## Open Questions

- Which Invoice Ninja client search/list endpoint is the best fit for a responsive client selector in this deployment, and does it require pagination or server-side search?
- Which Invoice Ninja invoice fields should carry `job number` and line `unit` in the deployed version: native fields, configured custom fields, or both?
- Should non-draft invoices allow PDF/download/send actions from inside the dialog footer, or should those remain list-level actions only for the first delivery?
- Should the invoice detail endpoint return the fully editable canonical Invoice Ninja payload, or should JB2026 expose a normalized dialog DTO tailored to the UI?
- How strict should numeric validation be for qty and unit cost with respect to decimal precision and zero values?
### Suggested Answers & Decisions

- **Client endpoint**: Use `/clients/search` with query param for server‑side filtering; paginate 20 results per request.
- **Custom fields**: Map `job number` and `unit` to Invoice Ninja custom fields `job_number` and `unit`. Store mapping in backend config.
- **Non‑draft actions**: Expose PDF/Download/Send only as read‑only footer buttons; keep list‑level actions as primary.
- **DTO vs raw payload**: Return a normalized DTO; backend handles field translation.
- **Numeric precision**: Enforce two‑decimal precision; reject values with more than two decimals.
## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Scope overlaps prior Invoice Ninja v1 boundaries | Existing lifecycle spec said post-create editing was deferred | Explicitly modify `invoice-lifecycle-mgmt` to narrow editing to draft-only manual invoices in Billing view |
| Client-side divergence from Invoice Ninja payload shape | UI could save fields the backend cannot map safely | Use a normalized backend DTO for create/update and keep Invoice Ninja mapping logic server-side |
| Draft status goes stale while dialog is open | User edits a draft that was already sent elsewhere | Revalidate status on save and reject updates for non-draft invoices with a stable business error |
| Large client lists hurt dialog responsiveness | Picker could be slow or unusable | Use backend search/filter endpoint and lazy loading instead of shipping all Invoice Ninja client data to the browser |
| Calculation mismatches between UI and backend | Totals displayed before save may differ from authoritative totals | Keep UI totals deterministic from entered values and return authoritative saved totals from backend to refresh the list |
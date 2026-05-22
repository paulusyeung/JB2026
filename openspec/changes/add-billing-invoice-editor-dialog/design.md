## Context

`BillingInvoicesView.vue` already owns the main billing list experience: toolbar actions, invoice selection, summary rendering, and navigation to invoice detail routes. It now includes `New Invoice`, `Mark Sent`, and `Download` actions, but `New Invoice` still routes away from billing and invoice-number clicks navigate to a separate detail page rather than opening an in-context editor.

The broader Invoice Ninja integration already established JB2026.Api as the only component allowed to talk to Invoice Ninja directly. It also originally deferred broad invoice editing after creation. This change should extend that contract carefully: the billing list gains a shared dialog for create/edit/view, while the backend continues to own every Invoice Ninja read/write and remains the sole source of truth for saved invoices.

## Goals / Non-Goals

**Goals:**
- Open a shared dialog from the billing list for invoice create, draft edit, and non-draft read-only view.
- Replace the current `New Invoice` redirect with in-place invoice authoring.
- Load existing invoice details into the dialog when an invoice number is clicked.
- Allow manual entry of client, invoice date, job number, and repeated line items with `P.O.Number`, description, qty, unit, unit cost, calculated line total, and invoice total.
- Save directly to Invoice Ninja through backend billing endpoints.
- Make the dialog fully i18n-ready and return refreshed list data after save.

**Non-Goals:**
- Local draft persistence in browser storage or any `ClientApp`-owned invoice datastore.
- Editing non-draft invoices.
- Full Invoice Ninja invoice management parity such as delete, void, partial payment capture, or template editing.
- Replacing the existing list-level `Mark Sent` and `Download` actions in this first delivery.

## Decisions

### 1) Use one shared dialog component with explicit mode: create, edit, view
- Decision: introduce one billing invoice dialog surface that receives a mode derived from the trigger context: `create` for `New Invoice`, `edit` for clicked invoices with status `Draft`, and `view` for clicked invoices with any other status.
- Rationale: the form structure is the same across all three flows; mode-specific behavior should be controlled by field disablement and footer actions rather than duplicated components.
- Alternative considered: separate create and read-only detail views. Rejected because it would split one conceptual workflow across different surfaces and duplicate rendering logic.

### 2) Make the backend expose a normalized invoice-editor DTO
- Decision: add billing API endpoints that return a normalized dialog payload instead of forwarding raw Invoice Ninja invoice JSON directly to the browser.
- Rationale: Invoice Ninja payloads are broader than the dialog needs and may vary across versions. A focused DTO lets the backend normalize client identity, invoice date, job number, line items, and totals into a stable UI contract.
- Alternative considered: bind the UI directly to raw Invoice Ninja invoice payloads. Rejected because it leaks external schema details into the client and makes version drift harder to contain.

### 3) Restrict updates to invoices whose authoritative backend status is still Draft
- Decision: the save path for an existing invoice must re-fetch or otherwise validate authoritative invoice status before update; non-draft invoices are served read-only and update attempts are rejected with a stable business error.
- Rationale: read-only gating in the UI is necessary for usability but is insufficient for concurrency safety.
- Alternative considered: optimistic save based only on the list row status. Rejected because the list summary can go stale while the dialog is open.

### 4) Keep line calculations in the UI, but treat backend totals as authoritative after save
- Decision: each line total is computed in the dialog as `qty * unitCost`, and the invoice total is the sum of all line totals. After save, the returned billing summary/detail becomes authoritative for the list refresh.
- Rationale: users need immediate feedback while editing, but Invoice Ninja remains the source of truth after persistence.
- Alternative considered: request server-side recalculation on every line edit. Rejected due to latency and unnecessary API chatter.

### 5) Retrieve Invoice Ninja clients through backend-assisted search/select
- Decision: the dialog's client picker will load Invoice Ninja-backed client options from a backend billing endpoint, with support for server-side filtering if the list is large.
- Rationale: client records live in Invoice Ninja and may be too large or dynamic to preload into the billing list page.
- Alternative considered: hydrate all clients when `BillingInvoicesView` loads. Rejected because it couples list load time to client-directory size.

### 6) Save create and update operations directly to Invoice Ninja; no client-side persistence
- Decision: `ClientApp` stores only transient in-memory dialog state. Save operations post to new backend billing endpoints that create or update the Invoice Ninja invoice and then refresh the billing list from the returned summary/detail payload.
- Rationale: this preserves Invoice Ninja as the billing authority and avoids a split-brain draft model between browser state and the backend.
- Alternative considered: stage unsaved drafts locally and sync later. Rejected because the user explicitly asked that invoices not be stored on `ClientApp`.

### 7) Make dialog strings and validation fully i18n-ready from the first delivery
- Decision: all dialog labels, placeholders, column headers, validation errors, mode titles, and action labels should use locale keys under the existing billing namespace.
- Rationale: this view already uses `useI18n`, and retrofitting hardcoded strings later would be noisy and easy to miss.
- Alternative considered: add English strings first and localize later. Rejected because the requested behavior is a high-visibility workflow surface.

## Proposed API Shape

- `GET /api/v2/billing/clients`
  - Returns a filtered list of Invoice Ninja client options for the selector.
- `GET /api/v2/billing/invoices/{externalInvoiceId}`
  - Returns normalized invoice editor detail for the dialog.
- `POST /api/v2/billing/invoices`
  - Creates a new Invoice Ninja invoice from the normalized dialog payload.
- `PUT /api/v2/billing/invoices/{externalInvoiceId}`
  - Updates an existing Invoice Ninja invoice only when it is still `Draft`.

Example normalized payload direction:

```ts
type BillingInvoiceEditorDto = {
  externalInvoiceId?: string
  status?: string
  client: {
    externalClientId: string
    displayName: string
  } | null
  invoiceDate: string | null
  jobNumber: string
  lineItems: Array<{
    id?: string
    poNumber: string
    description: string
    qty: number
    unit: string
    unitCost: number
    lineTotal: number
  }>
  totalAmount: number
}
```

The server may ignore incoming client-computed totals and recompute them before forwarding to Invoice Ninja.

## UI Flow

1. User clicks `New Invoice`.
2. `BillingInvoicesView` opens the shared dialog in `create` mode with one empty line item row.
3. User selects an Invoice Ninja client, sets invoice date, enters job number, and edits line items.
4. The dialog computes line totals and invoice total live.
5. User clicks `Save`; frontend posts the normalized payload to the backend.
6. Backend creates the Invoice Ninja invoice and returns normalized summary/detail.
7. The billing list updates in place and the dialog closes.

Existing invoice flow:

1. User clicks an invoice number in the billing list.
2. Frontend requests the normalized detail payload.
3. If status is `Draft`, dialog opens in `edit` mode and fields remain writable.
4. If status is not `Draft`, dialog opens in `view` mode and all form controls are disabled/read-only.
5. If the user saves a draft edit, the backend revalidates draft status and updates Invoice Ninja.

## Risks / Trade-offs

- [This change widens the original Invoice Ninja v1 scope] -> Mitigation: keep the expansion narrow to draft-only manual edits and explicitly leave non-draft mutation out of scope.
- [A normalized DTO can drift from Invoice Ninja capabilities] -> Mitigation: keep the DTO intentionally small and version it inside the billing service layer if needed.
- [Dialog complexity can bloat `BillingInvoicesView.vue`] -> Mitigation: move form rendering and line-item logic into a dedicated component/composable rather than embedding everything in the list view.
- [Validation differences between frontend and backend could frustrate users] -> Mitigation: enforce the same required fields in both places and surface backend validation messages through localized UI slots.

## Migration Plan

1. Add normalized billing invoice editor contracts and endpoints in `JB2026.Api`.
2. Implement Invoice Ninja client lookup, invoice detail mapping, invoice create, and draft-only update in the billing service.
3. Add frontend billing service methods and types for client lookup and invoice editor save/load.
4. Introduce the shared invoice dialog component and line-item editing logic.
5. Rewire `BillingInvoicesView.vue` so `New Invoice` and invoice-number clicks use the dialog instead of route navigation.
6. Add i18n keys across supported locales.
7. Add focused tests for dialog mode gating, calculations, save flows, and stale-draft rejection.

## Open Questions

- Should the dialog expose due date now, or derive it in Invoice Ninja from client terms until a later change requests explicit due-date editing?
- Is the `job number` best stored only in the configured Invoice Ninja invoice custom field, or should it also appear in `public_notes`/`private_notes` for operator visibility?
- Do we need a separate unsaved-changes confirmation when closing the dialog, or can the first delivery rely on simple cancel/close behavior?
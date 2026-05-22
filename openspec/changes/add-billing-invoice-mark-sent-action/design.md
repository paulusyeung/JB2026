## Context

`BillingInvoicesView.vue` already supports checkbox selection, billing list sorting, and status rendering for Invoice Ninja-backed invoice summaries. The client service layer currently exposes list, summary, and refresh operations, while `BillingController` and `IBillingService` provide the corresponding backend proxy operations. There is no action that changes an invoice lifecycle state after creation, so operators must leave JB2026 to send a draft invoice from Invoice Ninja.

Because Invoice Ninja credentials are backend-owned and the billing list already renders normalized status values (`Draft`, `Sent`, `Viewed`, `Paid`, `Overdue`), this change should extend the existing billing proxy surface instead of adding any direct frontend-to-Invoice-Ninja integration.

## Goals / Non-Goals

**Goals:**
- Add a `Mark Sent` action to `BillingInvoicesView` after `New Invoice`.
- Keep the action disabled unless exactly one selected invoice is eligible to transition from `Draft` to `Sent`.
- Send the invoice through the backend-owned Invoice Ninja integration and return normalized summary data.
- Refresh the list state immediately after success so the row status changes to `Sent` without manual reload.
- Surface stable validation and failure messages for stale or invalid selections.

**Non-Goals:**
- Bulk-send multiple invoices in one click.
- Support sending invoices from statuses other than `Draft`.
- Expose Invoice Ninja credentials, direct API calls, or email-template controls in the frontend.
- Redesign the broader Billing Invoices toolbar or replace current checkbox selection behavior.

## Decisions

### 1) Gate the action on exactly one selected draft invoice
- Decision: enable `Mark Sent` only when `checkboxMode` is active, exactly one invoice is selected, and that invoice's normalized status equals `Draft` case-insensitively.
- Rationale: the requested action is singular, the existing table/card selection model can hold multiple IDs, and a single-target rule avoids undefined behavior when mixed statuses are selected.
- Alternative considered: enable whenever any selected invoice is draft. Rejected because it creates ambiguous behavior for mixed selections and implies a batch contract the backend does not yet support.

### 2) Add a focused billing send endpoint in the JB2026 API
- Decision: add `POST /api/v2/billing/invoices/{externalInvoiceId}/send` returning `{ billingSummary, sentAt }`.
- Rationale: the billing controller already owns list/detail/refresh invoice operations, so a send action belongs on the same resource path and keeps Invoice Ninja credentials server-side.
- Alternative considered: overload the existing refresh endpoint with a mode flag. Rejected because refresh is read-oriented and should stay side-effect free.

### 3) Validate draft status on the backend before and after the Invoice Ninja send call
- Decision: the billing service will fetch the invoice summary if needed, reject non-draft invoices with a stable business error, perform the Invoice Ninja send operation, then return an authoritative normalized `InvoiceBillingSummary` from the Invoice Ninja response or a follow-up fetch.
- Rationale: frontend gating improves usability, but the backend must still enforce the lifecycle rule because selections can go stale between list rendering and click time.
- Alternative considered: trust only the frontend status check. Rejected because concurrent status changes in Invoice Ninja or another JB2026 session would make that unsafe.

### 4) Update the list immediately from the returned summary and clear the stale selection
- Decision: on success, replace the matching invoice in `invoices` with the returned `billingSummary`, clear `selectedInvoiceIds`, and let the existing computed sort/view pipeline re-render the row or card.
- Rationale: the response already contains the authoritative status, so patching one row is cheaper and faster than reloading the entire list while still meeting the instant-refresh requirement.
- Alternative considered: call `loadInvoices()` after every successful send. Rejected as the default path because it adds avoidable latency and refreshes unaffected rows.

### 5) Disable the action while the send request is in flight
- Decision: introduce an in-flight guard for the `Mark Sent` button and ignore duplicate clicks until the request resolves.
- Rationale: Invoice send is a side-effecting operation against an external system, and duplicate requests would be hard to reason about.
- Alternative considered: allow repeated clicks and rely on backend idempotence. Rejected because the current integration does not document send idempotence.

## Risks / Trade-offs

- [Invoice Ninja send endpoint semantics may differ from create/list calls] -> Mitigation: isolate the external send request inside `IBillingService` and return normalized `BillingException` errors to the controller.
- [Frontend selection can become stale between list load and action click] -> Mitigation: revalidate draft status in the backend and return a conflict-style business error when the invoice is no longer draft.
- [Single-select gating is narrower than the current checkbox UI suggests] -> Mitigation: keep the button disabled for multi-select and document batch send as a possible follow-up change.
- [Local row patch can drift if Invoice Ninja mutates more fields than status] -> Mitigation: patch from the authoritative response payload, not from a locally inferred status string.

## Migration Plan

1. Extend billing API models and `IBillingService` with a send-invoice request/response contract.
2. Add `POST /api/v2/billing/invoices/{externalInvoiceId}/send` to `BillingController`.
3. Implement the Invoice Ninja send flow in `BillingService`, including draft validation and normalized summary mapping.
4. Extend `ClientApp/src/services/billing.ts` with a send-invoice method.
5. Add the `Mark Sent` button and enablement/pending/success/error handling in `BillingInvoicesView.vue`.
6. Add targeted API and frontend tests for draft gating, failure cases, and immediate status refresh.

## Open Questions

- Which exact Invoice Ninja action endpoint should the service use for the send transition in the deployed Invoice Ninja version, and does it return a full invoice payload or require a follow-up fetch?

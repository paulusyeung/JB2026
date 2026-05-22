## Why

Billing Invoices already surfaces Invoice Ninja-backed invoice summaries, but operators cannot transition a draft invoice to sent from the list itself. That forces them into the external Invoice Ninja workflow for a routine status change and breaks the intended billing control point inside JB2026.

## What Changes

- Add a `Mark Sent` toolbar button in `BillingInvoicesView` immediately after `New Invoice`.
- Keep the button disabled by default and enable it only when at least one selected invoice is in `Draft` status; for the first delivery, the action targets a single selected draft invoice.
- Add a backend-mediated action that instructs Invoice Ninja to send the selected draft invoice, changing its status from `Draft` to `Sent` without exposing Invoice Ninja credentials to the web client.
- Refresh the Billing Invoices list immediately after a successful action so the updated status appears in-place.
- Show a stable error message when the selected invoice cannot be sent because it is no longer draft, the invoice is missing, or Invoice Ninja rejects the request.

## Capabilities

### New Capabilities
- `billing-invoice-send-action`: Send a draft Invoice Ninja invoice from the Billing Invoices list and refresh the list with the updated status.

### Modified Capabilities
- None.

## Impact

- Frontend UI: `JB2026.WebApp/ClientApp/src/views/BillingInvoicesView.vue` toolbar, selection gating, and post-action refresh behavior.
- Frontend services: billing service types and API call for the send action.
- Backend/API: new or extended Invoice Ninja proxy endpoint in `JB2026.Api` to send a draft invoice and return updated billing summary data.
- Invoice Ninja integration: relies on the existing backend-owned Invoice Ninja authentication/configuration path.
- Testing: targeted UI, service, and API coverage for enablement rules, draft-only sending, and refreshed status rendering.

## Open Questions

- **Invoice Ninja “send” endpoint** – Which exact Invoice Ninja API method should be called and does it return a full invoice payload or require a follow‑up fetch?
- **Idempotency & concurrency** – How should the backend respond if two users attempt to send the same draft concurrently? Should it be idempotent or return a conflict?
- **Error‑message UX** – What level of detail should be shown to the operator (raw Invoice Ninja error codes vs. user‑friendly messages)?
- **Future bulk‑send design** – Should the new endpoint be designed now to accept multiple IDs for later bulk support?
- **Testing strategy** – Do we need integration tests against a sandbox Invoice Ninja instance, or are unit‑test mocks sufficient?

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Stale selection | User selects a draft, but it changes before the request is sent, leading to 404/conflict | Backend re‑validates draft status; client can include a version check before sending |
| Invoice Ninja rate limits | Rapid sends may hit API throttling, causing failures | In‑flight guard on button; surface “try again later” on 429 |
| Partial payload updates | Response may omit fields the UI expects, causing stale UI data | Patch row with full response; fallback to list reload if needed |
| Security leakage | Accidental logging of invoice IDs or credentials | Ensure logs sanitize secrets; only log operation IDs |
| UI clutter | Adding another toolbar button may crowd the toolbar | Place the action in an overflow menu or group under “More” |
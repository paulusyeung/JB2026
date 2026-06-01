## Context

`BillingStatementView` already lists Invoice Ninja clients and gates the `Statement` toolbar button to exactly one checked client, but the click handler still terminates in a localized placeholder message. The closest existing billing patterns in this repo already keep Invoice Ninja credentials and request construction on the backend, with ClientApp calling `JB2026.Api` through `ClientApp/src/services/billing.ts` for client lookup, invoice editing, send, and download operations.

This change crosses both the Vue billing UI and the backend billing integration. Invoice Ninja already exposes a dedicated client-statement upstream endpoint at `POST /api/v1/client_statement` that accepts `client_id`, `start_date`, `end_date`, `show_payments_table`, `show_credits_table`, and `show_aging_table`, and returns the generated client statement output. This change therefore needs a dialog-driven request flow, a normalized request contract, a safe way to open the resulting statement in a new tab, and a server-side adapter that can translate the UI's statement options into that upstream contract without exposing Invoice Ninja credentials or brittle endpoint details to the browser.

## Goals / Non-Goals

**Goals:**
- Replace the placeholder `Statement` click behavior with a real request dialog in `BillingStatementView`.
- Let the user choose the required presets and selectors before launching the statement.
- Keep Invoice Ninja credentials, request parameter mapping, and statement retrieval fully backend-owned.
- Open the generated statement in a separate browser tab while keeping the current billing list intact.
- Add focused tests for gating, defaults, payload mapping, launch behavior, and request failure handling.

**Non-Goals:**
- Redesign the billing statement list layout, columns, or selection model beyond the already-approved view.
- Support multi-client statement generation in one action.
- Introduce arbitrary custom date pickers, saved presets, or persisted user preferences for the dialog.
- Email, print, or otherwise post-process the statement beyond opening the generated result in a new tab.

## Decisions

1. Use a dedicated billing statement request dialog instead of overloading the list view
- Decision: Add a focused dialog component/composable for the `Statement` action rather than expanding the toolbar into inline controls or reusing the billing invoice editor dialog shell.
- Rationale: The requested inputs are small, action-scoped, and only relevant at launch time. A dedicated dialog keeps `BillingStatementView` readable and avoids coupling statement filters to invoice-editor-specific state and layout behavior.
- Alternatives considered:
  - Inline toolbar filters: rejected because they crowd the list view and make the action state harder to reason about.
  - Reusing `BillingInvoiceEditorDialog`: rejected because the statement request flow is materially simpler and does not share the editor's data model.

2. Use a backend-issued launch URL for the new-tab flow
- Decision: `Proceed` will call a new backend billing endpoint that validates the selected client and request options, maps them to Invoice Ninja `POST /api/v1/client_statement`, produces a short-lived launch token or launch URL owned by `JB2026.Api`, and returns that URL to the browser. The frontend then opens that URL in a new tab, and the backend GET endpoint streams or redirects to the Invoice Ninja statement result using inline browser rendering semantics.
- Rationale: New-tab behavior works best with a navigable URL, not a JSON-embedded file payload. A backend-owned launch URL keeps Invoice Ninja authentication server-side, allows one-time-use or short-lived tokens, and avoids leaking raw Invoice Ninja URLs or API tokens to ClientApp.
- Alternatives considered:
  - Return the full file/blob payload from the initial POST: rejected because it complicates browser new-tab rendering and increases payload size on the interactive request.
  - Call Invoice Ninja directly from ClientApp: rejected because credentials and endpoint adaptation must remain backend-owned.

3. Keep dialog options as normalized UI enums and boolean selectors
- Decision: The frontend will submit a normalized statement request DTO containing the selected `externalClientId`, a constrained `dateRangePreset`, a constrained `status`, and boolean flags for `includeCredits`, `includePayments`, and `includeAging`. The backend will map `externalClientId` to Invoice Ninja `client_id`, map date presets to `start_date` and `end_date`, and map the selectors to `show_credits_table`, `show_payments_table`, and `show_aging_table`.
- Rationale: The UI requirements are preset-based, not free-form. A normalized DTO keeps the view stable even though the upstream Invoice Ninja contract uses a different field shape.
- Alternatives considered:
  - Expose raw Invoice Ninja query parameter names to the client: rejected because it couples ClientApp to integration details and increases breakage risk.
  - Convert the preset to explicit start/end dates in the browser: rejected because `All Outstanding` is not a pure calendar range and the mapping belongs with the backend integration adapter.

4. Treat the requested `Status` filter as a JB2026-level concern because Invoice Ninja client statements do not expose a native status parameter
- Decision: Keep the `Status` control in the user-facing dialog, but document that Invoice Ninja `POST /api/v1/client_statement` does not accept a `status` field. The backend implementation must therefore either translate `Paid` and `Unpaid` into JB2026-side launch rules compatible with the upstream statement request or fail fast with an explicit unsupported-option response until the business chooses the intended behavior.
- Rationale: The upstream endpoint is suitable for the core statement generation feature, but it does not fully cover the requested filter set. Calling that mismatch out now avoids falsely implying that `Paid` and `Unpaid` are native Invoice Ninja statement options.
- Alternatives considered:
  - Remove the `Status` field from the proposal: rejected because the user explicitly requested it.
  - Pretend the upstream endpoint supports `status`: rejected because it would make the spec inaccurate.

5. Default dropdown values are explicit, selector toggles are opt-in
- Decision: The dialog will default `Date Range` to `All Outstanding`, `Status` to `All`, and the `Credits`, `Payments`, and `Aging` selectors to unchecked.
- Rationale: The dropdown defaults are explicitly requested, while leaving the optional selectors unchecked is the least surprising baseline and avoids silently broadening the generated statement contents.
- Alternatives considered:
  - Default all selectors to checked: rejected because it changes statement output more aggressively than the request specifies.
  - Persist the last used selector state locally: rejected because preference persistence is out of scope for this first workflow slice.

6. Open a placeholder tab synchronously before awaiting the launch response
- Decision: The frontend should open a blank browser tab/window at the moment the user clicks `Proceed`, then replace that tab's location with the backend launch URL when the POST succeeds. If the request fails, the placeholder tab should be closed and the dialog should surface an inline error.
- Rationale: Browsers are more likely to allow new tabs created directly from a user gesture than tabs opened after an awaited async call. This approach improves reliability without weakening the backend-owned integration boundary.
- Alternatives considered:
  - Wait for the POST response, then call `window.open`: rejected because popup blockers are more likely to prevent the tab.
  - Navigate the current tab to the statement result: rejected because the requirement explicitly asks for a new tab and the current list state should remain visible.

## Risks / Trade-offs

- [Risk] Invoice Ninja `POST /api/v1/client_statement` may return content or metadata differently across deployed versions despite the current docs. -> Mitigation: isolate upstream request/response handling in the backend billing service and validate against the deployed environment during implementation.
- [Risk] The requested `Status` filter is not part of the upstream Invoice Ninja client statement API. -> Mitigation: keep `Status` as an explicit design constraint and require the implementation to either define a backend translation strategy or return a stable unsupported-option error until product behavior is clarified.
- [Risk] Popup blockers may still interfere with delayed tab navigation in some browsers. -> Mitigation: open the placeholder tab synchronously on `Proceed` and close it on failure.
- [Risk] The generated statement may be HTML or PDF depending on Invoice Ninja behavior. -> Mitigation: the backend launch endpoint should preserve the upstream content type and return inline-friendly response headers.
- [Risk] The selected client could become stale between list load and statement launch. -> Mitigation: revalidate the target client and return a stable business error before attempting the upstream request.

## Migration Plan

1. Add backend DTOs, controller actions, and billing service methods for statement launch and statement retrieval.
2. Extend the Invoice Ninja HTTP client/service layer with any new POST and stream helpers needed for `POST /api/v1/client_statement`.
3. Add ClientApp billing service types plus the new statement request dialog and integrate it into `BillingStatementView`.
4. Add i18n strings and focused API/web-app tests for the statement request slice.
5. Rollback plan: remove the new billing statement launch endpoints and revert `BillingStatementView` to the existing placeholder action while leaving the underlying statement list intact.

## Open Questions

- How should JB2026 interpret the requested `Status` values `Paid` and `Unpaid`, given that Invoice Ninja `POST /api/v1/client_statement` does not expose a native status filter?
- Should the backend return a one-time tokenized launch URL, or is a short-lived signed URL sufficient for the deployed security model?
- Does the business want the three optional selectors to remain stateless defaults, or should a later enhancement persist the last-used values per browser session?
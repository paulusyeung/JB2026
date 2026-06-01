## Context

ClientApp already has two relevant patterns for this change. `AdminCustomerView` provides the desired list layout, toolbar structure, and color treatment, while the existing Billing area already owns Invoice Ninja-backed APIs and navigation. The backend billing service also already exposes a client-list method used by the invoice editor, but that contract currently targets client selection and does not yet carry the statement-list fields needed for a balance-oriented table.

This change is intentionally a UI-and-contract foundation slice. It adds a new Billing menu entry and statement list view, aligns the page visually with `AdminCustomerView`, switches the data source to Invoice Ninja clients, and introduces the toolbar `Statement` button with enablement rules only. The click workflow behind that button is explicitly deferred so this proposal can establish the route, payload shape, formatting rules, and selection contract without coupling to downstream statement generation behavior.

## Goals / Non-Goals

**Goals:**
- Add a new `Statement` entry under the Billing menu and register a matching ClientApp route.
- Implement a new statement list view that follows the `AdminCustomerView` layout pattern, theme treatment, and i18n-ready structure.
- Populate the view from Invoice Ninja clients through the existing billing integration surface.
- Replace the admin-customer-specific credential columns with an `Outstanding Balance` column formatted as requested.
- Preserve the first four toolbar controls and divider from the source pattern, then add a persistent `Statement` button that is enabled only for exactly one checked client.
- Add focused tests around navigation, data mapping, balance formatting, and selection gating.

**Non-Goals:**
- Generating, previewing, emailing, downloading, or otherwise executing a client statement when the `Statement` button is clicked.
- Reusing Admin Customer backend endpoints or customer login/password fields in the new Billing statement view.
- Introducing multi-currency formatting rules beyond the requested fixed `$` display convention.
- Redesigning the Billing menu structure beyond adding the new `Statement` entry.

## Decisions

1. Reuse the `AdminCustomerView` interaction shell, not the `BillingInvoicesView` shell
- Decision: The new Billing statement screen will follow the structural pattern from `AdminCustomerView`, including the same list-first layout, color treatment, checkbox selection behavior, and first four toolbar controls plus divider.
- Rationale: The request explicitly points to `AdminCustomerView` as the desired baseline, and that view already matches the needed list ergonomics better than the invoices screen.
- Alternatives considered:
  - Extend `BillingInvoicesView` into a generic billing-list shell: rejected because its invoice-specific actions and data model create more cleanup than reuse.
  - Build a fully new layout: rejected because it would drift from the requested visual baseline.

2. Extend the existing billing client-list contract instead of creating a second Invoice Ninja client endpoint
- Decision: Reuse the existing billing client-list pathway and extend its DTO/model mapping to expose the additional fields needed by the statement page, especially outstanding balance and any identifiers shown in the table.
- Rationale: The backend already has Invoice Ninja connectivity and a client lookup flow, so extending that contract minimizes duplicate mapping logic and keeps Invoice Ninja access backend-owned.
- Alternatives considered:
  - Introduce a separate statement-only endpoint with parallel client mapping: rejected because it duplicates existing Invoice Ninja client retrieval behavior.
  - Query Invoice Ninja directly from ClientApp: rejected because credentials and integration logic must remain on the server.

3. Keep balance formatting client-side, with a fixed display contract
- Decision: The API will return raw balance values, and the view will render them left-aligned with a leading `$`, comma grouping, and two decimal places.
- Rationale: Returning raw numeric values preserves sorting/filtering flexibility and keeps presentation rules localized to the view layer.
- Alternatives considered:
  - Return preformatted balance strings from the API: rejected because it reduces reuse and makes numeric sorting harder.
  - Use per-client currency symbols from Invoice Ninja: rejected for this proposal because the requested display contract is explicitly `$`-prefixed and a broader multi-currency design would widen scope.

4. Treat the toolbar `Statement` button as a gated placeholder action in this change
- Decision: The button will be visible at all times, disabled unless exactly one client is checked, and wired only as far as shared selection state and UI affordance in this proposal.
- Rationale: This matches the requested staged rollout: establish the selection contract now, then attach the real statement workflow in a follow-up proposal.
- Alternatives considered:
  - Hide the button until a single client is selected: rejected because the request asks for a normally disabled persistent action.
  - Implement a provisional statement action now: rejected because the user explicitly deferred that behavior.

## Risks / Trade-offs

- [Risk] Invoice Ninja client responses used today may not expose balance in the current mapped DTO. -> Mitigation: extend the existing backend model and add a focused mapping test before wiring the view.
- [Risk] The fixed `$` format may not reflect every Invoice Ninja client currency. -> Mitigation: keep the requirement explicit in this proposal and document multi-currency handling as a later enhancement if needed.
- [Risk] Copying `AdminCustomerView` too literally can leak irrelevant admin-only columns or actions. -> Mitigation: define the statement column set and toolbar composition explicitly in the spec and tests.
- [Risk] An enabled-but-deferred `Statement` action can confuse users if it appears unfinished. -> Mitigation: keep the scope explicit in the design and implement only the enablement contract in this change; the follow-up proposal will attach the workflow.

## Migration Plan

1. Extend the backend billing client-list contract to include the fields required by the statement view.
2. Add frontend service/types for the statement list and register the Billing menu entry and route.
3. Implement the new statement view using the approved layout, column, and toolbar rules.
4. Add i18n strings and focused frontend/backend tests.
5. Rollback plan: remove the Billing Statement route/menu entry and view while leaving the underlying client-list contract available for other billing features if regressions appear.

## Open Questions

- Should the new statement route path be `/billing/statement` or `/billing/statements` to match current naming conventions best?
- When the user clicks the enabled `Statement` button in this change, should the UI do nothing, show a neutral placeholder message, or simply reserve the handler for the follow-up proposal?
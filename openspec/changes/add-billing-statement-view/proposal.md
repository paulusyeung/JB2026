## Why

Billing currently exposes invoice-oriented screens, but it does not provide a statement-oriented client list that lets operators review Invoice Ninja client balances in the same list-driven pattern already used elsewhere in ClientApp. This is needed now to add the Billing menu entry and establish the shared UI, data contract, and selection gating before the actual statement action workflow is proposed separately.

## What Changes

- Add a new Billing menu entry and route for `Statement`.
- Add a new statement list view that follows the `AdminCustomerView` layout pattern, color treatment, and i18n-ready structure.
- Source the statement list from Invoice Ninja clients rather than JB2026 admin customers.
- Remove the `Login Account` and `Password` columns from the reused list pattern.
- Add an `Outstanding Balance` column that renders the Invoice Ninja client balance left-aligned with a leading `$`, thousands separators, and two decimal places.
- Keep the first four toolbar actions and the divider from the existing list pattern, then add a persistent `Statement` action after the divider.
- Keep the toolbar `Statement` action disabled by default and enable it only when exactly one client is checked.
- Defer the actual `Statement` button execution flow to a follow-up proposal; this change only establishes the view, routing, data binding, formatting, and enablement behavior.

## Capabilities

### New Capabilities
- `billing-statement-view`: Billing statement client list, navigation entry, Invoice Ninja client sourcing, balance formatting, and single-selection statement action gating.

### Modified Capabilities
- None.

## Impact

- Frontend navigation: Billing route registration and Billing menu group entries in ClientApp.
- Frontend UI: a new statement list view based on the `AdminCustomerView` interaction pattern and theme treatment.
- Frontend services/types: Invoice Ninja client list DTOs and billing service methods that expose client balances to the view.
- Localization: new `routes.*` and `billing.statement.*` translation keys.
- Backend/API: billing-facing endpoint or existing proxy expansion that returns Invoice Ninja clients with the fields required by the statement list.
- Testing: focused coverage for route/menu visibility, column set, balance formatting, and `Statement` button enablement rules.
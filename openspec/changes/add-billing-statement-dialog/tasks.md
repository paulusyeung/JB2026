## 1. Backend Statement Launch Contract

- [x] 1.1 Define billing statement request/response DTOs for selected client, date-range preset, status filter, selector flags, and launch URL/token payload, including the mapping to Invoice Ninja `client_id`, `start_date`, `end_date`, and `show_*_table` fields.
- [x] 1.2 Add billing controller endpoints to create a statement launch request and to serve the resulting statement resource in a new tab.
- [x] 1.3 Extend the billing service and Invoice Ninja HTTP client with the request/stream helpers needed to call Invoice Ninja `POST /api/v1/client_statement` and stream the resulting statement output.
- [x] 1.4 Define and implement the backend behavior for the user-facing `Status` filter because Invoice Ninja client statements do not provide a native status parameter.
- [x] 1.5 Add backend validation and error mapping for missing client selection, unsupported option values, stale clients, unresolved `Status` mappings, and upstream statement-generation failures.

## 2. Billing Statement Dialog UI

- [x] 2.1 Add a dedicated billing statement request dialog component or composable with the required dropdowns, selector toggles, and `Cancel`/`Proceed` actions.
- [x] 2.2 Integrate the dialog into `BillingStatementView` so the enabled `Statement` button opens the dialog instead of the current placeholder message.
- [x] 2.3 Implement the `Proceed` launch flow, including pending-state handling, duplicate-submit prevention, placeholder-tab creation, and navigation of that tab to the returned backend launch URL.
- [x] 2.4 Surface launch failures in the dialog without navigating away from the billing statement list and close any unusable placeholder tab on failure.

## 3. Localization And Verification

- [x] 3.1 Add `billing.statement.*` locale strings for dialog labels, presets, selector text, buttons, loading states, and launch errors across supported locales.
- [x] 3.2 Add focused frontend tests for statement button gating, dialog defaults, cancel behavior, request payload mapping, and successful new-tab launch behavior.
- [x] 3.3 Add focused backend tests for request validation, Invoice Ninja `client_statement` option mapping, launch URL/token generation, `Status` handling, and upstream failure handling.
- [x] 3.4 Run the relevant API and web-app validation for the billing statement dialog flow and record the results for the change.

	**Verification result (2026-06-02):**
	- Backend parity (`BillingClientStatementTests`): **5/5 passed** — request validation, unsupported status handling, Invoice Ninja `client_statement` payload mapping, launch URL generation, and upstream `503` propagation.
	- Frontend Playwright (`billing.statement.spec.ts`): **3/3 passed** — statement button gating, dialog defaults, cancel behavior, request payload mapping, and successful popup launch.
	- ClientApp typecheck: **blocked by unrelated pre-existing error** in `src/views/BillingInvoiceStatsView.vue` (`formatCurrency` declared but never read).
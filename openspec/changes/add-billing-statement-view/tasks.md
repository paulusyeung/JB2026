## 1. Billing Client Contract

- [x] 1.1 Extend the backend billing client-list DTO and mapping to include the fields required by the statement list, including outstanding balance.
- [x] 1.2 Add or update ClientApp billing service types and fetch helpers for the statement client list payload.
- [x] 1.3 Add focused backend/API coverage for Invoice Ninja client-to-statement-list mapping and balance values.

## 2. Statement Navigation And View

- [x] 2.1 Add the Billing `Statement` route and Billing menu entry with i18n-backed route labels.
- [x] 2.2 Implement the new billing statement list view using the `AdminCustomerView` layout and theme pattern while removing `Login Account` and `Password` from the column set.
- [x] 2.3 Add the `Outstanding Balance` column with left-aligned `$` formatting, comma grouping, and two decimal places.
- [x] 2.4 Preserve the first four toolbar controls and divider from the baseline list pattern, then add a persistent `Statement` button gated to exactly one checked client.

## 3. Localization And Verification

- [x] 3.1 Add `routes.billingStatement` and `billing.statement.*` locale strings for labels, headers, actions, and messages.
- [x] 3.2 Add focused frontend tests for menu visibility, route rendering, column exclusion, outstanding-balance formatting, and `Statement` button enablement rules.
- [x] 3.3 Run the relevant frontend and backend test suites for the billing statement slice and record the verification result for the change.

	**Verification result (2026-06-02):**
	- Backend parity (`BillingClientListTests`): **2/2 passed** — Invoice Ninja client name/display name fallback, client code, and outstanding balance mapping.
	- Frontend Playwright (`billing.statement.spec.ts`): **3/3 passed** — Billing menu/route visibility, exclusion of login/password columns, `$` balance formatting, and `Statement` button enablement for zero/one/multiple checked clients.
	- ClientApp typecheck: **blocked by unrelated pre-existing error** in `src/views/BillingInvoiceStatsView.vue` (`formatCurrency` declared but never read).
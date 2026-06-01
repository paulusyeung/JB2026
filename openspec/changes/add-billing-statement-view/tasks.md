## 1. Billing Client Contract

- [ ] 1.1 Extend the backend billing client-list DTO and mapping to include the fields required by the statement list, including outstanding balance.
- [ ] 1.2 Add or update ClientApp billing service types and fetch helpers for the statement client list payload.
- [ ] 1.3 Add focused backend/API coverage for Invoice Ninja client-to-statement-list mapping and balance values.

## 2. Statement Navigation And View

- [ ] 2.1 Add the Billing `Statement` route and Billing menu entry with i18n-backed route labels.
- [ ] 2.2 Implement the new billing statement list view using the `AdminCustomerView` layout and theme pattern while removing `Login Account` and `Password` from the column set.
- [ ] 2.3 Add the `Outstanding Balance` column with left-aligned `$` formatting, comma grouping, and two decimal places.
- [ ] 2.4 Preserve the first four toolbar controls and divider from the baseline list pattern, then add a persistent `Statement` button gated to exactly one checked client.

## 3. Localization And Verification

- [ ] 3.1 Add `routes.billingStatement` and `billing.statement.*` locale strings for labels, headers, actions, and messages.
- [ ] 3.2 Add focused frontend tests for menu visibility, route rendering, column exclusion, outstanding-balance formatting, and `Statement` button enablement rules.
- [ ] 3.3 Run the relevant frontend and backend test suites for the billing statement slice and record the verification result for the change.
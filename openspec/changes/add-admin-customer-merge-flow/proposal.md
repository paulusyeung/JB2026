## Why

Admin Customer management currently has no safe way to consolidate duplicate or superseded customer records once invoices and quotations already reference them. This is needed now to preserve billing and quotation history while letting operators retire redundant customers from a single merge flow in the existing toolbar.

## What Changes

- Add a persistent `Merge` action to the `AdminCustomerView` toolbar and mobile overflow actions.
- Keep `Merge` disabled by default and enable it only when two or more customers are checked.
- Add a merge dialog that shows the selected customers, allows the operator to choose exactly one merge target, and requires explicit confirmation before execution.
- Add a backend merge workflow that reassigns `InvoiceHeader.CustomerId` and `QtHeader.CustomerId` from non-target customers to the chosen target customer.
- Retire all non-target customers in the merge set by setting `Customer.Retired = true`, `Customer.RetiredOn = today`, and `Customer.RetiredBy = the authenticated login user`.
- Refresh the customer list and selection state after merge, and surface success or failure feedback in context.
- Add frontend and backend tests for enablement rules, dialog constraints, reference reassignment, and retirement behavior.

## Capabilities

### New Capabilities
- `admin-customer-merge`: Merge selected admin customers into one surviving customer while preserving invoice and quotation references and retiring the source customers.

### Modified Capabilities
- None.

## Impact

- Affected UI modules:
  - `JB2026.WebApp/ClientApp/src/views/AdminCustomerView.vue`
  - Customer dialog/service/composable code used by admin customer actions
- Affected backend/data modules:
  - Admin customer API/controller/service path for merge execution
  - Data-access code touching `Customer`, `InvoiceHeader`, and `QtHeader`
  - Auth/user-context access used to stamp `RetiredBy`
- Affected tests:
  - Frontend tests for toolbar button state, dialog selection rules, and post-merge refresh
  - Backend correctness/parity tests for customer-reference reassignment and retirement stamping
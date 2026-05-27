## 1. Frontend Merge Contract And State

- [x] 1.1 Add admin customer merge request types and a `mergeAdminCustomers` service wrapper for `POST /api/v2/admin/customers/merge`.
- [x] 1.2 Add shared merge state in `AdminCustomerView` for dialog visibility, target selection, loading, and `canMergeSelectedCustomers` gating.
- [x] 1.3 Add i18n strings for the merge button, dialog content, validation hints, and success/failure feedback.

## 2. Admin Customer Merge UX

- [x] 2.1 Add a persistent `Merge` action to the desktop toolbar and mobile overflow actions, disabled unless two or more customers are selected.
- [x] 2.2 Implement the merge dialog so it lists only selected customers and enforces exactly one surviving target selection.
- [x] 2.3 Wire merge confirmation to the admin service, then close the dialog, refresh the list, and keep the surviving target selected after success.

## 3. Backend Merge Verification And Hardening

- [x] 3.1 Verify the existing `AdminController.MergeCustomers` flow matches the spec for validation, transactional reassignment of `InvoiceHeader`/`QtHeader`, and retirement stamping.
- [x] 3.2 Close any gaps found in the existing merge endpoint without changing the contract unless tests prove it is necessary.
- [x] 3.3 Add or extend API tests for invalid target selection, missing or retired customers, successful reference reassignment, and `RetiredOn`/`RetiredBy` stamping.

## 4. Frontend Tests And Verification

- [x] 4.1 Add frontend tests for merge-button visibility, disabled/enabled state transitions, and shared desktop/mobile gating.
- [x] 4.2 Add frontend tests for dialog single-target behavior, merge submit enablement, and post-success refresh behavior.
- [x] 4.3 Run the relevant frontend and API test suites for admin customer merge and record the verification result for the change.

  **Verification result (2026-01-01):**
  - API (`CustomerMergeCorrectnessTests`): **6/6 passed** — invalid target, missing customer, retired customer, successful reassignment (InvoiceHeader + QtHeader), RetiredOn/RetiredBy stamping, multi-source merge.
  - Frontend (`admin.customer-merge.spec.ts`): **9/9 passed** — button visibility, disabled when no selection, disabled with one selection, enabled with two+, dialog lists selected customers, confirm disabled until target chosen, single-target enforcement, post-merge refresh.
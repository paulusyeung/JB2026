## Context

`AdminCustomerView.vue` already supports checkbox-based multi-selection, but its toolbar only exposes create, view, and billing-sync actions. The backend already contains `POST /api/v2/admin/customers/merge` in `AdminController`, and that endpoint validates a distinct customer set, requires the target customer to be one of the selected customers, reassigns `InvoiceHeader.CustomerId` and `QtHeader.CustomerId`, retires the non-target customers, and stamps `RetiredBy` from the authenticated actor.

The missing pieces are the user-facing merge affordance, a frontend request contract/service wrapper, confirmation UX that lets the operator choose exactly one surviving customer, and test coverage around both the UI rules and the existing server-side merge semantics. Because merge is destructive and irreversible at the customer-record level, the design needs an explicit confirmation path and must keep desktop/mobile enablement rules aligned.

## Goals / Non-Goals

**Goals:**
- Add a persistent `Merge` action to `AdminCustomerView` desktop and mobile actions.
- Keep merge unavailable unless at least two customers are selected.
- Present a dialog that lists the selected customers and allows exactly one merge target.
- Reuse the existing backend merge endpoint and align the frontend contract to it.
- Verify that merge preserves the target customer, reassigns invoice/quotation headers, retires source customers, and refreshes the list state after completion.
- Add focused tests for toolbar enablement, dialog constraints, API invocation, and backend merge correctness.

**Non-Goals:**
- Merging customer field content such as bill-to, ship-to, login account, or customer code from source customers into the target record.
- Unretiring customers or providing an undo flow for merge.
- Auto-selecting the merge target based on heuristics or similarity scoring.
- Changing the existing backend merge contract from one surviving target plus retired sources.

## Decisions

1. Reuse the existing merge endpoint instead of introducing a new API shape
- Decision: The frontend will call the existing `POST /api/v2/admin/customers/merge` contract with `targetCustomerId` and `customerIds`.
- Rationale: The server already performs the required transactional updates and validations, so adding a second endpoint would duplicate logic and increase drift risk.
- Alternatives considered:
  - Introduce a new dedicated merge service/controller pair: rejected because the current endpoint already matches the requested semantics.
  - Perform reference updates from the client through multiple calls: rejected because merge must stay transactional and server-owned.

2. Single-target choice in the dialog
- Decision: The merge dialog will show only the currently selected customers and will permit exactly one target selection at a time.
- Rationale: The user requirement is to let the operator choose one surviving customer while retiring all others. Restricting the dialog to the selected set prevents accidental cross-list targeting and keeps the payload valid by construction.
- Alternatives considered:
  - Free-form customer lookup for the target: rejected because it broadens scope and weakens the explicit-selection safety model.
  - Multi-check target selection with later validation: rejected because the UI can prevent invalid states more cleanly.

3. Persistent button with disabled-state policy
- Decision: Render `Merge` persistently in the toolbar and mobile overflow actions, and gate it with a shared computed condition based on `selectedCustomerIds.length >= 2` and in-flight merge state.
- Rationale: This matches the discoverability pattern used by other list actions while still preventing invalid execution.
- Alternatives considered:
  - Only show `Merge` when two or more customers are selected: rejected because hidden destructive actions are harder to learn and drift from the requested disabled-default behavior.

4. Merge outcome and post-merge refresh policy
- Decision: On success, close the dialog, reload the customer list, clear non-existent source selections, and keep the surviving target selected when it remains visible in the refreshed list.
- Rationale: Operators need immediate confirmation that the merge completed and that retired source rows are no longer actionable.
- Alternatives considered:
  - Leave stale selections untouched: rejected because it leaves retired customers selected and creates ambiguous next actions.
  - Always clear selection entirely: rejected because retaining the surviving target helps operators continue working on the canonical record.

5. Backend hardening is verification-first, not redesign-first
- Decision: Treat the existing `AdminController.MergeCustomers` path as the implementation baseline and add tests around its current validation, transactional update, and retirement stamping behavior before considering structural refactoring.
- Rationale: The backend already appears to meet the requested semantics, so the highest-value work is proving and documenting that behavior while exposing it in the UI.
- Alternatives considered:
  - Refactor merge into a new service before exposing the feature: rejected for now because it increases scope without changing user-visible requirements.

## Risks / Trade-offs

- [Risk] Merge is irreversible for retired source customers and can be triggered on the wrong selection set. -> Mitigation: require explicit dialog confirmation and constrain the target to the selected set only.
- [Risk] Desktop and mobile actions can drift on enablement behavior. -> Mitigation: centralize `canMergeSelectedCustomers` logic and reuse it for both entry points.
- [Risk] Existing backend merge logic may lack coverage for edge cases such as retired targets, missing customers, or actor resolution. -> Mitigation: add API/controller tests that lock down current behavior before frontend rollout.
- [Risk] Keeping the merge endpoint inside `AdminController` preserves a large controller surface. -> Mitigation: accept this in the proposal scope and revisit refactoring only if tests expose maintainability problems.

## Migration Plan

1. Add frontend request/response types and service wrapper for customer merge.
2. Add merge action, dialog, and shared enablement/loading state in `AdminCustomerView`.
3. Wire success and failure handling, then refresh the list and selection state.
4. Add backend tests for merge validation and data reassignment semantics.
5. Add frontend tests for toolbar state, dialog single-target selection, and merge execution flow.
6. Rollback plan: remove the frontend action and dialog while leaving the backend endpoint internal-only if regressions appear.

## Open Questions

- Should the merge dialog show only customer name/code, or also login account and billing status to help users choose the surviving target?
- Should the success message report counts of merged and retired customers, or remain a simple single-message confirmation?
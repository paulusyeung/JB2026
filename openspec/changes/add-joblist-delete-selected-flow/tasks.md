## 1. Legacy Parity And Contract Definition

- [x] 1.1 Confirm Job List delete parity against JB2015 `JobList.cs` line 789 and `Utility.JobOrder.DeleteItem` cleanup semantics
- [x] 1.2 Confirm backend delete contract for Job Order lifecycle includes workflow cleanup, attachment metadata cleanup, and attachment file/image deletion
- [x] 1.2b Investigate current job-order attachment file storage location(s), access pattern(s), and any existing path resolution/deletion utilities (before implementing 2.2)
- [x] 1.3 Confirm post-delete sibling job-number rebuild behavior and acceptance criteria
- [x] 1.4 Confirm intentional UX divergences from JB2015 (no active-row fallback; Delete visible+disabled even outside checkbox mode)

## 2. Backend Job Order Delete Lifecycle

- [x] 2.1 Implement or align Job Order delete service flow to delete related job workflow rows before deleting job order
- [x] 2.2 Implement or align attachment cleanup flow to delete storage files/images and attachment rows in a guarded server-side orchestration
- [x] 2.3 Implement or align job-order delete operation and sibling job-number rebuild logic
- [x] 2.4 Return delete outcome payloads/error codes that allow aggregate success/failure reporting in Job List UI
- [x] 2.5 Decide whether storage cleanup failures are warnings (logged, still 2xx) vs hard failures, and codify behavior
- [x] 2.6 Ensure sibling renumbering uses fresh per-item DB state during batch deletes (avoid stale jobNumber snapshots)

## 3. Job List UI Delete Action Parity

- [x] 3.1 Update Job List toolbar delete action to remain visible and disabled when no selected job exists
- [x] 3.2 Apply the same delete enable/disable criteria in mobile overflow actions
- [x] 3.3 Keep delete action disabled during delete execution and show loading state while processing
- [x] 3.4 Ensure selection-first behavior is consistent with StockView interaction model
- [x] 3.5 Update i18n to support aggregate success/failure counts (not just a binary "one or more failed" message)

## 4. Job List Delete Execution And Feedback

- [x] 4.1 Wire delete action to selected job ids with explicit confirmation prompt
- [x] 4.2 Process multi-selection delete sequentially and continue after item-level failures
- [x] 4.3 Show aggregate outcome feedback for success/failure counts and failure fallback messaging
- [x] 4.4 Refresh Job List rows and clear selected ids after delete completion

## 5. Tests And Verification

- [x] 5.1 Add frontend tests for delete button visibility, disabled state without selection, and enabled state with selection
- [x] 5.2 Add frontend tests for confirmation-cancel and confirmation-accept delete flows
- [x] 5.3 Add frontend tests for batch delete mixed outcomes and post-delete refresh/selection clearing
- [x] 5.4 Add backend parity/correctness tests for workflow cleanup, attachment row cleanup, and attachment file/image deletion
- [x] 5.5 Add backend tests for sibling job-number rebuild after delete
- [ ] 5.6 Run relevant regression/parity tests and document sign-off evidence for Job List delete parity
- [x] 5.7 Update/extend API controller/repository test doubles affected by changes to `DeleteJobOrder` contract/behavior

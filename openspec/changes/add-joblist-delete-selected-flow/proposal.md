## Why

Job List needs a first-class Delete action for selected job orders that matches the current Stock page interaction model: visible action button with disabled state until a selection exists. This is needed now to close a core parity gap with legacy Job List delete behavior and to ensure deletion removes both database records and attachment image files.

## What Changes

- Add a persistent Delete action in Job List toolbar and mobile overflow actions, aligned with StockView interaction style:
  - Button is shown in normal operation.
  - Button remains disabled when no valid selection exists.
  - Button enters loading state while delete is processing.
- Support selected-item delete execution from Job List with checkbox mode and single-selection parity.
- Port legacy Job List delete semantics from JB2015 (`JobList.cs` line 789 and `Utility.JobOrder.DeleteItem`):
  - Delete selected item(s) after explicit confirmation.
  - Delete related workflow rows.
  - Delete related job attachment rows and corresponding files/images from storage.
  - Delete the Job Order record.
  - Rebuild job numbering for remaining sibling items after delete.
- Define/align backend delete orchestration so Job List delete guarantees database + storage cleanup in one server-side lifecycle.
- Add user feedback and refresh behavior to keep list rows, selection state, and counts in sync after delete.
- Add or extend tests for UI enablement rules, confirmation path, and cleanup parity.

## Capabilities

### New Capabilities
- `job-order-delete-lifecycle`: Enable parity-aligned Job List delete for selected job orders, including workflow cleanup, attachment record cleanup, and attachment image file cleanup.

### Modified Capabilities
- None.

## Impact

- Affected UI modules:
  - `JB2026.WebApp/ClientApp/src/views/JobListView.vue`
  - Job-order frontend services/composables used by Job List delete flow.
- Affected backend/data modules:
  - Job-order delete API/service path responsible for hard-delete lifecycle.
  - Job workflow and job attachment cleanup logic.
  - Attachment file storage cleanup orchestration.
  - Job-number rebuild behavior after delete.
- Affected tests:
  - Frontend behavior tests for delete-button enablement, confirmation, and post-delete refresh.
  - Backend parity/correctness tests for record + file cleanup and numbering rebuild behavior.

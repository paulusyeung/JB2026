## Context

`JobListView.vue` currently exposes delete only in checkbox mode and only when selected count is greater than zero (`v-if`), which differs from StockView where delete is persistently visible and disabled until a valid selection exists. The requested feature requires parity with StockView button behavior and parity with JB2015 legacy delete lifecycle, where `cmdDelete_Click` (line 789) executes selected-item deletion and delegates to `Utility.JobOrder.DeleteItem`.

Legacy `Utility.JobOrder.DeleteItem` behavior includes:
- Deleting related `JobWorkflow` rows.
- Deleting `JobAttachment` rows and deleting physical files via `AttachmentFilePath(...)`.
- Deleting the `JobOrder` row.
- Rebuilding sibling job numbers when needed.

The new flow must preserve this destructive-action safety model (explicit confirmation) and guarantee DB + storage cleanup from a server-side path, not from ad hoc client-side file handling.

## Goals / Non-Goals

**Goals:**
- Add a persistent Job List Delete button (desktop and mobile actions) aligned with StockView behavior.
- Keep Delete disabled unless at least one valid selected job exists.
- Support single and multi-selection delete execution from Job List.
- Ensure delete lifecycle removes Job Order data and attachment image files from storage.
- Preserve legacy parity for workflow cleanup and job-number rebuild behavior.
- Keep user-facing feedback clear for success/failure and refresh list/selection state after completion.

**Non-Goals:**
- Reworking unrelated Job List toolbar actions (print/export/views).
- Changing Job Order edit form UX.
- Introducing soft-delete/retire semantics for Job Orders (legacy behavior is immediate delete).
- Refactoring storage infrastructure outside delete orchestration needs.

## Decisions

1. Persistent button with disabled-state policy
- Decision: Render Delete action continuously in Job List actions and control interactivity using `:disabled="selectedOrderIds.length === 0 || deleting"`.
- Rationale: Matches StockView affordance and improves discoverability over conditional rendering.
- Alternatives considered:
  - Keep current conditional visibility (`v-if selected > 0`): rejected because users cannot discover action before selecting.
  - Permit active-row delete without selection: rejected to avoid accidental destructive actions and to keep one selection model.

2. Selection model parity with existing checkbox mode
- Decision: Delete targets `selectedOrderIds`; users must use selection state for explicit intent in both desktop and mobile flows.
- Rationale: Current Job List already tracks `selectedOrderIds` and has batch-delete semantics. This also standardizes destructive actions across Job List and StockView.
- Alternatives considered:
  - Use active row fallback when no checkbox selection (JB2015 behavior): rejected for ambiguity and mismatch with the requested selected-item behavior.

  **Note on legacy parity**: JB2015 `cmdDelete_Click` falls back to deleting the active row when checkbox-mode is not engaged or no checked rows exist. This change intentionally *does not* implement that fallback in order to keep one selection-first model and reduce accidental deletes.

3. Server-side delete orchestration owns DB + file cleanup
- Decision: Backend delete service/API shall own full lifecycle (workflow rows, attachment rows, file/image deletion, job-order delete, numbering rebuild) in one orchestrated operation.
- Rationale: Prevents partial cleanup and keeps storage side effects authoritative and testable.
- Alternatives considered:
  - Client-side file cleanup calls: rejected due to integrity and security risks.
  - Separate endpoint calls for DB delete and file delete: rejected due to partial-failure risk.

4. Batch processing policy and outcome reporting
- Decision: For multi-selection, process selected ids sequentially and continue after item-level failures; show aggregate outcome in UI.
- Rationale: Mirrors established destructive batch behavior in StockView and avoids fail-fast disruption for operators.
- Alternatives considered:
  - All-or-nothing transaction across all selected jobs: rejected for poor operator throughput and higher lock/failure blast radius.

  **Item-level failure definition**:
  - A job order delete is considered **failed** if the server returns a non-2xx response for that job id.
  - Storage cleanup issues (e.g. file delete throws) SHOULD be treated as **warnings** (logged server-side) and SHOULD NOT cause the request to fail if DB cleanup succeeded. This matches existing best-effort file deletion behavior used in stock hard-delete.

  **Warning surfacing**:
  - Decision (now): storage-cleanup warnings are **log-only** (no warning payload surfaced to the frontend) as long as DB cleanup succeeds.
  - Future option: add an optional warnings array to the delete response for observability without changing success semantics.

5. Confirmation-first destructive flow
- Decision: Keep explicit confirmation dialog before executing delete requests.
- Rationale: Legacy parity and destructive operation safety.
- Alternatives considered:
  - Snackbar undo pattern without confirmation: rejected because file deletion side effects are immediate and complex to reverse.

6. Delete visibility across modes
- Decision: Keep Delete action visible in the Job List toolbar/menu even when `checkboxMode` is off; disable it when there is no selection or when a delete is running.
- Rationale: Aligns with StockView interaction model and satisfies "visible + disabled without selection" requirement.

## Risks / Trade-offs

- [Risk] Partial failures between DB and storage cleanup can leave orphan files or rows. -> Mitigation: enforce server-side orchestration with transactional boundaries for DB operations and explicit error handling/logging for storage operations.
- [Risk] Batch deletes can take noticeable time with many selections. -> Mitigation: disable actions while deleting, show loading state, and report aggregate results.
- [Risk] Disabled-state logic drift between desktop and mobile menus. -> Mitigation: centralize `canDeleteSelected` computed gate and reuse across action entry points.
- [Risk] Legacy job-number rebuild semantics may regress ordering assumptions. -> Mitigation: add parity/correctness test coverage for post-delete numbering in sibling job orders.
- [Risk] Concurrent deletes of siblings for the same `OrderNumber` can race and produce temporary numbering anomalies. -> Mitigation: keep renumbering logic in a server-side function and add correctness tests; accept low-probability operational risk.

## Migration Plan

1. Add/confirm backend delete contract for Job Order cleanup parity (DB + storage + numbering).
2. Update Job List UI delete action rendering to persistent+disabled state pattern.
3. Wire delete action to selected ids with confirmation and aggregate feedback handling.
4. Refresh list data and selection state after delete completion.
5. Add frontend and backend parity/correctness tests for enablement, execution, and cleanup semantics.
6. Rollback plan: revert to non-executable delete action state while preserving current list rendering if regressions are detected.

## Open Questions

- Should the mobile overflow delete action use identical disabled criteria and loading indicator text as desktop, or only disabled state without loading visuals?
- Should aggregate batch results distinguish between DB delete failures and storage cleanup failures in user-facing messages?
- (Answered) Should the API surface storage-cleanup warnings (for observability) while still returning 2xx for successful DB cleanup? -> Not initially; warnings are log-only.

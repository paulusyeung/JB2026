## Context

`SchedulePendingView.vue` already renders row-level workflow and urgency indicators using `workflowColor` and `urgencyIcon`, but the toolbar lacks direct status action buttons. Legacy WinForms behavior in JB2015 `PendingList` exposed explicit toolbar actions for workflow lights (`light1_*`, `light2_*`) and urgency bells (`bell_red`, `bell_yellow`) that operate on the selected row(s), with buttons disabled unless a selection exists.

In JB2026, pending data is fetched from `GET /api/v2/job-schedules/pending`. The current scheduler API surface does not expose endpoints to mutate pending workflow-step colors or urgency bell color from this screen. The design therefore must cover both frontend controls and API write operations.

## Goals / Non-Goals

**Goals:**
- Add pending-toolbar icon actions that visually align with current workflow and urgency icon semantics.
- Keep action buttons disabled by default and enable only when exactly one active row is selected in pending view.
- Support click actions that persist selected-row workflow/urgency changes and immediately reflect the new values in the list.
- Preserve legacy-compatible behavior for bell actions (red/yellow toggle semantics).
- Define API and service contracts required by the toolbar actions.

**Non-Goals:**
- Re-implement legacy multi-select checkbox batch updates in this change.
- Redesign all pending view actions or replace current toolbar architecture.
- Add new workflow concepts beyond step status color and urgency bell color updates.

## Decisions

### 1) Introduce explicit pending action endpoints in JobSchedules API
- Decision: add focused endpoints under `JobSchedulesController` for row-level pending updates.
- Proposed routes:
  - `PATCH /api/v2/job-schedules/pending/{orderId}/workflow` for step status update.
  - `PATCH /api/v2/job-schedules/pending/{orderId}/urgency` for urgency bell update/toggle.
- Rationale: current API has read-only pending data and unrelated write endpoints; focused endpoints keep intent clear and avoid overloading existing batch/time routes.
- Alternative considered: reuse generic job/workflow endpoints if present. Rejected because no existing route currently supports this exact operation from pending schedule context.

### 2) Keep toolbar actions selection-gated and idempotent per click
- Decision: all new icon buttons default to disabled; enabled when `activeOrderId` resolves to a current row.
- Rationale: matches user requirement and prevents invalid writes.
- Alternative considered: enable by checkbox selection count. Rejected for now because this change targets selected-row behavior only.

### 3) Map button clicks to existing numeric status semantics used by pending rows
- Decision: represent workflow updates using step index + target status code compatible with current `step1Status/step2Status/step3Status` values.
- Decision: urgency action uses color target (`red`/`yellow`) with toggle logic compatible with legacy behavior: clicking the active bell color returns urgency to neutral/default.
- Rationale: preserves visual compatibility with `workflowColor` and `urgencyIcon` renderers without requiring UI remapping.
- Alternative considered: string enums in client state only. Rejected to avoid conversion drift and parity issues.

### 4) Apply optimistic-local row patch only after successful API response
- Decision: on success, patch the selected row in `rows` so users see immediate updates; on failure, keep data unchanged and show action notice.
- Rationale: balances responsiveness with data correctness.
- Alternative considered: force full reload after each action. Rejected due to unnecessary latency and focus loss.

## Risks / Trade-offs

- [Backend parity mismatch on urgency neutral value] -> Mitigation: define explicit API response contract returning normalized `urgencyLevel` after each update.
- [Legacy allowed multi-select updates; this change is single-row] -> Mitigation: document scope and keep extension path for batch mode in a follow-up change.
- [Concurrent updates can stale local row state] -> Mitigation: patch from authoritative response payload, not from assumed local math.
- [Icon action discoverability on small screens] -> Mitigation: include actions in mobile-compatible toolbar/overflow with same enablement rules.

## Migration Plan

1. Add API request/response DTOs and controller actions for pending workflow/urgency updates.
2. Add scheduler service methods in client app to call new endpoints.
3. Add toolbar icon buttons in pending view with disabled/enabled bindings to selected row state.
4. Wire click handlers to service calls and patch local selected row fields from API response.
5. Add tests for API handlers, service contracts, and UI enablement/update behavior.
6. Rollout behind normal deployment; rollback by reverting frontend buttons and disabling new endpoints (no schema migration expected).

## Open Questions

- Should this iteration include step 3 action buttons explicitly, or keep parity with legacy `light1` and `light2` only?
- Which urgency value is considered neutral in current API (`-1` vs another code), and should it render as empty bell or stop icon in pending toolbar context?
- Should action clicks require a confirmation dialog (legacy parity) or execute immediately (modern UX default)?

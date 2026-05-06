## Why

The pending schedule toolbar currently shows status/urgency icons in rows but does not provide direct toolbar actions to update those states. Users must open other screens or dialogs for simple status updates, which is slower than the legacy workflow and breaks operator muscle memory.

## What Changes

- Add workflow-light and urgency-bell icon actions to the pending schedule toolbar in the web client, visually aligned with the existing workflow and urgency icon semantics.
- Keep these action buttons disabled by default and enable them only when **exactly one** row is selected / active (table/card/mobile modes). (This change does not add batch/multi-select updates.)
- Implement click behavior so each action updates the selected row's workflow step status or urgency level, following legacy intent from Job.Book PendingList actions:
  - workflow light actions for step groups (e.g., red/yellow/green/blue where applicable)
  - urgency bell actions (red/yellow with toggle behavior)
- Update local list state immediately after a successful action so users see the selected row change without manual refresh.
- Add request/response handling and user feedback for action failures (workflow and urgency).
- Prevent accidental duplicate writes by disabling the clicked action(s) while the request is in-flight.
- Handle stale selections gracefully (e.g., order removed/changed server-side): show an error notice and keep the row state unchanged until the next refresh.

## Capabilities

### New Capabilities
- `pending-toolbar-status-actions`: Add enabled-on-selection toolbar icon actions in Schedule Pending view to update selected-row workflow and urgency state with legacy-compatible behavior.

### Modified Capabilities
- None.

## Impact

- Frontend UI: Schedule Pending toolbar and selected-row state handling in the Vue page.
- Frontend services: add/update API calls used to set workflow lights and urgency bell color for a specific order.
- Backend/API (if endpoint gaps exist): expose/update endpoints required by the new toolbar actions.
- Testing: add/extend unit and integration tests for enable/disable rules and action-update behavior.

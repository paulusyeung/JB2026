## 1. API Contract and Backend Endpoints

- [x] 1.1 Add request/response DTOs for pending workflow-step update and pending urgency update actions in the API project.
- [x] 1.2 Implement `PATCH /api/v2/job-schedules/pending/{orderId}/workflow` in `JobSchedulesController` to update target step status and return normalized pending status payload.
- [x] 1.3 Implement `PATCH /api/v2/job-schedules/pending/{orderId}/urgency` in `JobSchedulesController` with red/yellow toggle-to-neutral behavior and normalized urgency response.
- [x] 1.4 Add API validation and error responses for invalid orderId, invalid step index, and invalid status/urgency targets.

## 2. Frontend Service Layer

- [x] 2.1 Extend `ClientApp/src/types/api.ts` with request/response types for pending workflow and urgency action endpoints.
- [x] 2.2 Extend `ClientApp/src/services/scheduler.ts` with service methods to call the new pending workflow and urgency patch endpoints.
- [x] 2.3 Add unit tests (or service-level tests where available) covering successful and failed scheduler action requests.

## 3. Pending View Toolbar and Interaction

- [x] 3.1 Add new workflow-light and urgency-bell icon buttons to `SchedulePendingView.vue` toolbar, ordered with existing actions and matching row status icon semantics.
- [x] 3.2 Bind all new buttons to selection-gated enablement so they are disabled by default and enabled only when an active selected row exists.
- [x] 3.3 Implement click handlers that invoke scheduler action services and patch the selected row status fields from successful responses.
- [x] 3.4 Implement failure handling via existing notice patterns so failed updates leave row data unchanged and show an error message.

## 4. Verification and Parity Coverage

- [x] 4.1 Add/extend frontend tests for toolbar enabled/disabled state transitions across table/card/mobile row selection.
- [x] 4.2 Add/extend backend tests for workflow update persistence, urgency toggle semantics, and invalid input handling.
- [x] 4.3 Run parity-relevant test suites and targeted manual validation on pending schedule to confirm legacy-compatible behavior for lights and bells.

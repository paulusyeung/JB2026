## Purpose

Records legacy-format FCM push history (`FCMHistory` rows) and dispatches webhook events when a job moves through its lifecycle, so the FCMHistory read API and external webhook consumers continue to receive the same lifecycle notifications the legacy JB5 app produced.

## ADDED Requirements

### Requirement: Backend records job lifecycle push history
The backend SHALL write an `FCMHistory` row whenever a job passes through one of seven lifecycle events: job created, job scheduled, plate workflow ready, paper workflow ready, job completed, job invoiced, or job cost (COGS) filled. Each row SHALL use the legacy format.

#### Row layout
Each written row SHALL contain:
- `Topic` = `Device`
- `MessageTitle` = the legacy title for the event (`JB5 新增訂單` for created, `JB5 已排單` for scheduled, `JB5 有鋅` for plate ready, `JB5 有紙` for paper ready, `JB5 全單完成` for job completed, `JB5 已開發票` for job invoiced, `JB5 已填成本` for COGS filled)
- `MessageBody` = `"{OrderNumber}-{JobNumber}: {CustomerName}"` for all events (the COGS event is job-scoped on the job's cost field `OriginalSONumber`, which is the `'COGS'` box in the job order form)
- `RecipientList` = the targeted device tokens joined with commas
- `UserIdList` = the target record GUID repeated once per targeted device
- `DeliveredOn` = the time at which the event occurred
- `FCMHistoryId` = a new GUID

#### Scenario: Plate ready event writes legacy row
- **WHEN** the plate workflow step for an order is marked ready
- **THEN** an `FCMHistory` row is appended with `Topic="Device"`, `MessageTitle="JB5 有鋅"`, and `MessageBody` in `"{OrderNumber}-{JobNumber}: {CustomerName}"` form

#### Scenario: Paper ready event writes legacy row
- **WHEN** the paper workflow step for an order is marked ready
- **THEN** an `FCMHistory` row is appended with `Topic="Device"`, `MessageTitle="JB5 有紙"`, and the legacy message body

#### Scenario: Job completed event writes legacy row
- **WHEN** a scheduled job is marked completed
- **THEN** an `FCMHistory` row is appended with `Topic="Device"`, `MessageTitle="JB5 全單完成"`, and the legacy message body

#### Scenario: Job created event writes legacy row
- **WHEN** a new job order is saved
- **THEN** an `FCMHistory` row is appended with `Topic="Device"`, `MessageTitle="JB5 新增訂單"`, and the legacy message body

#### Scenario: Job scheduled event writes legacy row
- **WHEN** a job is saved onto the schedule board
- **THEN** an `FCMHistory` row is appended with `Topic="Device"`, `MessageTitle="JB5 已排單"`, and the legacy message body

#### Scenario: Job invoiced event writes legacy row
- **WHEN** a job acquires invoice reference data, whether the user fills the invoice number in the job order form and saves, or an invoice is marked sent and the invoice data is persisted to the job
- **THEN** a single `FCMHistory` row is appended with `Topic="Device"`, `MessageTitle="JB5 已開發票"`, and the legacy message body

#### Scenario: COGS filled event writes legacy row
- **WHEN** the cost value of a job is set or changed (the `OriginalSONumber` cost field saved from the job order form)
- **THEN** an `FCMHistory` row is appended with `Topic="Device"`, `MessageTitle="JB5 已填成本"`, and `MessageBody` in `"{OrderNumber}-{JobNumber}: {CustomerName}"` form

### Requirement: Recipient device targeting
For each event the backend SHALL target devices. The target is the job owner's devices for all events, including COGS. A device is targeted when it is a registered device of the target (`UserAuth` with `AuthType=3`) and the target opted the device into the matching notify type for that event (`UserNotification.NotifyType`: 10 for created, 11 for scheduled, 12 for paper ready, 13 for plate ready, 14 for job completed, 15 for job invoiced, 16 for COGS filled). `RecipientList` SHALL list each targeted device token once, and `UserIdList` SHALL repeat the target record GUID once per targeted device.

#### Scenario: Owner with opted-in devices is targeted
- **WHEN** an event fires for an order whose owner has registered, opted-in devices
- **THEN** `RecipientList` contains exactly those device tokens and `UserIdList` repeats the owner GUID the same number of times

#### Scenario: Owner without opted-in devices falls back to all registered devices
- **WHEN** an event fires for an order whose owner has registered devices but none opted into the event
- **THEN** all of the owner's registered devices are targeted

#### Scenario: COGS event targets the job owner
- **WHEN** a job's cost value is set or changed
- **THEN** `UserIdList` contains the job owner's record GUID repeated once per targeted device from that owner's registered devices, exactly as for the other job-scoped events

#### Scenario: No target devices falls back to staff history
- **WHEN** an event fires whose target cannot be resolved or has no registered devices
- **THEN** a single `FCMHistory` row is still recorded with `RecipientList="staffonly"` and the event's legacy `MessageTitle`

### Requirement: Job lifecycle webhook events
Each lifecycle event SHALL dispatch a webhook through the existing subscription mechanism. Webhook event names SHALL be `OnJobCreated`, `OnJobScheduled`, `OnReadyPlate`, `OnReadyPaper`, `OnJobCompleted`, `OnJobInvoiced`, and `OnJobCogsFilled`. Existing subscriptions matching `OnReadyPaper`/`OnReadyPlate` SHALL continue to receive ready events.

#### Scenario: Ready event dispatches webhook with existing name
- **WHEN** the plate or paper workflow step for an order is marked ready
- **THEN** a webhook with event type `OnReadyPlate` or `OnReadyPaper` respectively is dispatched to all matching active subscriptions

#### Scenario: New lifecycle events dispatch webhooks
- **WHEN** a job is created, scheduled, completed, invoiced, or its cost (COGS) is filled
- **THEN** a webhook with event type `OnJobCreated`, `OnJobScheduled`, `OnJobCompleted`, `OnJobInvoiced`, or `OnJobCogsFilled` respectively is dispatched to all matching active subscriptions

### Requirement: Payloads include event subject identity
The payload dispatched with each webhook SHALL include the event subject identity, the event topic, and the event timestamp. For all events the subject is the order id. The writing of the `FCMHistory` row and the webhook dispatch SHALL both happen for the same event.

#### Scenario: Webhook payload carries subject identity
- **WHEN** a lifecycle event dispatches a webhook
- **THEN** the payload contains the subject order id, the event name, and the occurrence time

### Requirement: Only one record per event occurrence
The backend SHALL record each lifecycle event occurrence exactly once, even when the job is updated through multiple code paths. A job transition that is not a lifecycle event (e.g., a workflow step set ready when already ready, an order updated without being completed, or an invoice reference updated that was already set) SHALL NOT produce a new record.

#### Scenario: Repeated ready status does not duplicate history
- **WHEN** a workflow step already marked ready is set ready again
- **THEN** no additional `FCMHistory` row is written

#### Scenario: Completion transition records once
- **WHEN** an order transitions from not-completed to completed through either the schedule save or the job update path
- **THEN** exactly one `FCMHistory` row and one `OnJobCompleted` webhook result from that transition

#### Scenario: Invoice reference transition records once
- **WHEN** a job with no invoice reference acquires invoice data, whether through the job order form or the invoice mark-sent flow
- **THEN** exactly one `FCMHistory` row and one `OnJobInvoiced` webhook result from that transition, and saving the same invoice reference again records nothing
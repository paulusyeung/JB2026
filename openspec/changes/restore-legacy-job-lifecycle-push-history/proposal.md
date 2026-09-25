## Why

The legacy JB5 app wrote `FCMHistory` rows (`Topic="Device"`, device-token `RecipientList`, owner `UserIdList`) each time a job moved through its lifecycle (created, scheduled, plate ready, paper ready, full completion) as history of its per-device push notifications. The JB2026 backend never recreated these rows, so the push-history feature that the current FCMHistory read API (`GET api/FCMHistory`) is designed to surface no longer gets populated. JB2026 also gained a webhook system that should announce the same job lifecycle events. The lifecycle events are: job created, job scheduled, plate workflow ready (green dot), paper workflow ready (green dot), job completed, job invoiced, and job cost (COGS) filled.

## What Changes

- Add a shared job-lifecycle event publisher (`JB2026.EfCore`) that writes legacy-format `FCMHistory` rows for seven events: `OnJobCreated`, `OnJobScheduled`, `OnReadyPlate`, `OnReadyPaper`, `OnJobCompleted`, `OnJobInvoiced`, and `OnJobCogsFilled`.
- Legacy-format rows set `Topic="Device"`, `MessageTitle` to the legacy title (`JB5 新增訂單`, `JB5 已排單`, `JB5 有鋅`, `JB5 有紙`, `JB5 全單完成`, `JB5 已開發票`, `JB5 已填成本`), `MessageBody` to `"{OrderNumber}-{JobNumber}: {CustomerName}"` for all events, `RecipientList` to the target's device tokens, `UserIdList` to the owner GUID repeated per device, and `DeliveredOn` to the event time.
- Recipients resolve to the job owner's devices that opted into the matching notify type (`UserNotification.NotifyType` 10–16), for every event. Fall back to all registered devices, then to the current `staffonly` row so history is always recorded.
- Webhook dispatch is made available in both hosts: a shared `IWebhookEventDispatcher` interface; `JB2026.Rest` keeps its Hangfire-backed dispatcher, `JB2026.Api` gets a synchronous dispatcher.
- Hook the publisher into the existing mutation points: job creation (`EfJobManagementRepository.CreateJobOrder`), scheduling batch save + mark-completed (`JobSchedulesController.SaveBatch`), schedule ready-status update (`ScheduleCompatibilityController.PostRegister`), and job invoice data persisted (InvoiceRef filled in the job order form update or via invoice mark-sent), and job COGS filled (the `OriginalSONumber` cost field saved from the job order form in `EfJobManagementRepository.CreateJobOrder`/`UpdateJobOrder`).
- Replace the `JB2026.Rest` `FcmEventHelperService` ready-event recording with the shared publisher; ready rows change from `Topic="OnReadyPaper"/"OnReadyPlate"` with `RecipientList="staffonly"` to legacy `Topic="Device"` rows (**BREAKING** for any consumer relying on the current topic values of these rows; webhook event names `OnReadyPaper`/`OnReadyPlate` are preserved).
- No real FCM/device delivery is added (record + webhook only, matching the current backend scope).

## Capabilities

### New Capabilities
- `notifications/job-lifecycle-push-history`: The backend records legacy-format `FCMHistory` rows and dispatches webhook events whenever a job is created, scheduled, has its plate or paper workflow become ready, is completed, is invoiced, or has its cost (COGS) filled.

### Modified Capabilities
<!-- No existing capability specs live under openspec/specs/ (directory is empty), so no MODIFIED
     requirement specs are declared. If a main spec later exists for FCM history recording, this
     change supplies the delta. -->

## Impact

- **Code**: `JB2026.EfCore` (new `JobLifecycleEventPublisher`, `JobLifecycleEventType`, `IWebhookEventDispatcher`, legacy title/device-resolution logic), `JB2026.Rest` (`FcmEventHelperService`/`IFcmEventHelperService` delegation, `ScheduleCompatibilityController` ready hooks, `WebhookDispatcherService` implements shared interface, `Program.cs` DI), `JB2026.Api` (`EfJobManagementRepository.CreateJobOrder` and `UpdateJobOrder` invoice + COGS transitions, `JobSchedulesController.SaveBatch`, `BillingService` mark-sent persist, new synchronous webhook dispatcher, startup DI registration, `UserCompatibilityController.MapNotifyType` extended with 15/16).
- **Tests**: Update `JB2026.Rest.Tests/ScheduleCompatibilityControllerTests` (ready rows now `Topic="Device"`); add device-resolution/row-composition unit tests and Api integration tests for the created/scheduled/completed hooks in `JB2026.Api.ParityTests`.
- **Data**: Writes to existing `FCMHistory` table only; no schema changes. Rows remain visible to the owner via `GET api/FCMHistory` (`UserIdList.Contains(userSid)` already matches legacy `Device` rows).
- **Config**: No new configuration (webhook dispatch reuses existing `WebhookSubscriptions`); Api adds an `HttpClient` for synchronous dispatch.
- **Dependencies**: No new NuGet packages.
## 1. Shared emitter infrastructure (JB2026.EfCore)

- [x] 1.1 Add `JobLifecycleEventType` enum (OrderCreated, Scheduled, ReadyPlate, ReadyPaper, Completed, Invoiced, CogsFilled) in `JB2026.EfCore/Notifications/` and verify the project builds (`dotnet build JB2026.EfCore`)
- [x] 1.2 Add `IWebhookEventDispatcher` interface (`EnqueueEventAsync(string eventType, object payload, CancellationToken)`) in `JB2026.EfCore/Notifications/` and verify it compiles
- [x] 1.3 Add legacy-title + NotifyType lookup maps for the seven events in the publisher and verify `dotnet build JB2026.EfCore` succeeds
- [x] 1.4 Implement `JobLifecycleEventPublisher` in `JB2026.EfCore` with owner resolution (`CreatedBy` GUID → fallback `OrderedBy` string match via `vwUserList_Active`), device filtering (`UserAuth.AuthType=3` ∩ `UserNotification.NotifyType`), and order-scoped `MessageBody` composition (`"{OrderNumber}-{JobNumber}: {CustomerName}"`) — verify `dotnet build JB2026.EfCore` succeeds
- [x] 1.5 Cover the COGS-filled event via the order-scoped publisher path (event tied to the `JobOrder`, order body, owner targeting, webhook payload carries the order id) — verify `dotnet build JB2026.EfCore` succeeds
- [x] 1.6 Add a unit-testable pure component for row/recipient building (owner/actor devices → `RecipientList`/`UserIdList`, staffonly fallback) and verify it is covered by a new unit test in the Api test project

## 2. Webhook dispatch available in both hosts

- [x] 2.1 Make `JB2026.Rest` `WebhookDispatcherService` implement the shared `IWebhookEventDispatcher` (keep Hangfire behaviour) and register in `Program.cs`, overriding/replacing the namespace references — verify `dotnet build JB2026.Rest` and `dotnet test JB2026.Rest.Tests` pass
- [x] 2.2 Add `SynchronousWebhookDispatcher` in `JB2026.Api` (reads `WebhookSubscriptions`, POSTs JSON via a named `HttpClient` with a short timeout, logs non-2xx/errors only) and register it with a named client — verify `dotnet build JB2026.Api` succeeds
- [x] 2.3 Add a `SynchronousWebhookDispatcher` test verifying it ignores invalid URLs, posts JSON to matching subscriptions, and tolerates non-2xx responses — verify the new test passes

## 3. Rest ready-event path (OnReadyPlate / OnReadyPaper)

- [x] 3.1 Update `IFcmEventHelperService`/`FcmEventHelperService` to delegate ready events to the shared publisher (legacy `Device` rows + webhooks `OnReadyPlate`/`OnReadyPaper`) — verify `dotnet build JB2026.Rest` succeeds
- [x] 3.2 Update `ScheduleCompatibilityControllerTests` to assert `Topic="Device"` and legacy `MessageTitle` for both ready events — verify `dotnet test JB2026.Rest.Tests` passes
- [x] 3.3 Search the repo for dependencies on the old `Topic="OnReadyPaper"/"OnReadyPlate"` history values and update or file them (`InMemory`/tests/docs) — verify no remaining references assert the old topic values

## 4. Api lifecycle hooks

- [x] 4.1 Inject the publisher into `EfJobManagementRepository` and emit `OnJobCreated` after the first `SaveChangesAsync` in `CreateJobOrder` — verify `dotnet build JB2026.Api` succeeds and an integration test shows one `Device` row + `OnJobCreated` webhook after create
- [x] 4.2 Inject the publisher into `JobSchedulesController` and emit `OnJobScheduled` per `ScheduledItems` item after the schedule upsert in `SaveBatch` (not for completed/cancelled items) — verify `dotnet build JB2026.Api` and an integration test covering a scheduled item
- [x] 4.3 Emit `OnJobCompleted` in `SaveBatch` completed block after `JobOrder.Status=2` + `SaveChangesAsync`, guarded by a not-completed → completed transition check — verify `dotnet build JB2026.Api` and an integration test covering mark-completed
- [x] 4.4 Emit `OnJobCompleted` in `EfJobManagementRepository.UpdateJobOrder` on the not-completed → completed transition, with no double record when `SaveBatch` already completed the order — verify `dotnet build JB2026.Api` and a transition test asserting exactly one row
- [x] 4.5 Emit `OnJobInvoiced` in `EfJobManagementRepository.UpdateJobOrder` when `InvoiceRef`/`InvoiceAmount` transition from empty to set (job order form save) — verify `dotnet build JB2026.Api` and a transition test asserting exactly one row + `OnJobInvoiced` webhook
- [x] 4.6 Emit `OnJobInvoiced` on the `BillingService` mark-sent persist path (where invoice data is written to the job), deduped so the form-save and mark-sent paths can't double-record for the same job — verify `dotnet build JB2026.Api` and a test covering mark-sent
- [x] 4.7 Emit `OnJobCogsFilled` in `EfJobManagementRepository.CreateJobOrder` (when `OriginalSONumber` is set — the `'COGS'` field saved from the job order form) and `UpdateJobOrder` (when `OriginalSONumber` changes to a set value) after `SaveChangesAsync`, targeting the job owner's devices — verify `dotnet build JB2026.Api` and tests covering job COGS create/update (no row on unchanged OriginalSO)
- [x] 4.8 Add NotifyType 15 (`onjobinvoiced`) and 16 (`onjobcogsfilled`) to `UserCompatibilityController.MapNotifyType` and verify `dotnet build JB2026.Api`
- [x] 4.9 Verify per-event `NotifyType` opt-in targeting (10/11/12/13/14/15/16) with a test using seeded `UserAuth` + `UserNotification` rows, confirming `RecipientList` and `UserIdList` shapes — verify the test passes

## 5. Integration verification

- [x] 5.1 Run `dotnet build JB2026.sln` and fix any compile errors across Api, Rest, EfCore, and test projects
- [x] 5.2 Run `dotnet test JB2026.Api.ParityTests` and `dotnet test JB2026.Rest.Tests` and confirm all tests pass
- [ ] 5.3 Manually exercise one end-to-end ready event against a local `JB2026.Rest` with a seeded active `WebhookSubscription` for `OnReadyPaper` and confirm a `Device` history row appears and a webhook POST arrives
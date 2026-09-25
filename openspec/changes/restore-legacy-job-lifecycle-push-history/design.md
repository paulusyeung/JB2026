## Context

See proposal.md (Why). The relevant current state:

- `FcmEventHelperService` (`JB2026.Rest/Helpers`) currently writes `FCMHistory` rows for the two ready events with `Topic="OnReadyPaper"/"OnReadyPlate"` and `RecipientList="staffonly"`, and dispatches webhooks via `IWebhookDispatcherService` (Hangfire in `JB2026.Rest`). Rows written this way are **not** surfaced by `FcmHistoryCompatibilityController.BuildVisibleQuery` (it matches `Topic="everyone"/"staffonly"` or `UserIdList.Contains(userSid)`).
- The seven lifecycle events fire in two projects. Ready events: `ScheduleCompatibilityController.PostRegister` (`JB2026.Rest`, `type==0` → paper, `type==1` → plate at `WorkStatus == 2`). Created, scheduled, completed, invoiced, and COGS events: `JB2026.Api` (`EfJobManagementRepository.CreateJobOrder` for created + COGS-on-create, `JobSchedulesController.SaveBatch` for scheduled/completed, `EfJobManagementRepository.UpdateJobOrder` for completed/invoiced/COGS transitions, `BillingService.SendInvoiceAsync` mark-sent persist for invoiced).
- Project dependency is one-way: `JB2026.Rest → JB2026.Api`. `JB2026.Api` cannot reference Rest helpers; the shared emitter must live in a common dependency (`JB2026.EfCore`, already referenced by both).
- Production container runs `JB2026.Api` (per `Dockerfile`); `JB2026.Rest` is the compatibility layer.
- Legacy row shape (from production `FCMHistory`): `Topic="Device"`, `MessageTitle` = legacy Chinese title, `MessageBody` = `"{OrderNumber}-{JobNumber}: {CustomerName}"`, `RecipientList` = comma-joined device tokens, `UserIdList` = owner GUID repeated per device.
- Device/opt-in data: `UserAuth` (`AuthType=3` = FCM registration), `UserNotification` (per-device `NotifyType` opt-in: 10 created, 11 scheduled, 12 paper, 13 plate, 14 completed, 15 invoiced, 16 COGS). Notify types 15–16 are additions to `UserCompatibilityController.MapNotifyType`. The job's COGS is the cost value stored as `JobOrder.OriginalSONumber` — the COGS text box in the job order form (`JobOrderForm.vue`, i18n label `'COGS'`) and the JobList "cogs" column both read this field (see `EfJobManagementRepository.ParseCostFromOriginalSoNumber`). It is set from the job order form via `CreateJobOrderRequest.OriginalSONumber`/`UpdateJobOrderRequest.OriginalSONumber`; there is no stock `Product` involvement.

## Goals / Non-Goals

**Goals:**
- Emit legacy-format `FCMHistory` rows + webhooks for all seven events from a single shared implementation.
- Make the rows surfaced by the existing FCMHistory read API visible to the owner (matching legacy behavior).
- Preserve webhook subscription compatibility for the ready events.
- Record exactly one row per event occurrence, across the multiple code paths that can touch a job or its invoice data.

**Non-Goals:**
- Real FCM/device delivery (no Firebase credentials/outbound push).
- Schema changes, new packages, or new configuration surfaces.
- Recreating legacy rows for historical jobs (only new events going forward).

## Decisions

1. **Shared emitter in `JB2026.EfCore`.** Add `JobLifecycleEventPublisher` (and `JobLifecycleEventType` enum) in `JB2026.EfCore/Notifications/`, taking `JB5LegacyWriteContext`, a device/order resolver, and `IWebhookEventDispatcher`. The publisher exposes a single order-scoped method (`PublishOrderEventAsync(eventType, orderId)`) used for all seven events, including the COGS event. Rationale: both `JB2026.Api` and `JB2026.Rest` already reference `JB2026.EfCore`; keeps a single source of truth for legacy row composition. Alternative rejected: putting it in `JB2026.Api` and calling from Rest would work (Rest → Api) but Api is the production container and needs to emit created/scheduled/completed/invoiced/COGS without depending on the Rest project.

2. **Shared `IWebhookEventDispatcher`; host-specific implementations.** Interface in `JB2026.EfCore` mirrors today's `EnqueueEventAsync(eventType, payload, ct)`. `JB2026.Rest` keeps `WebhookDispatcherService` (Hangfire-backed) implementing the interface; `JB2026.Api` gets `SynchronousWebhookDispatcher` (reads `WebhookSubscriptions` from the write context, POSTs JSON via an `HttpClient` with a short timeout, non-2xx/errors logged only — same tolerance as today's dispatcher). Rationale: preserves the existing async dispatch in Rest without dragging Hangfire into `JB2026.Api`; the sync semantics match the existing `WebhookDispatcherService` non-Hangfire fallback path. Alternative rejected: moving Hangfire packages into a shared project — larger dependency churn for no behavioral gain.

3. **Recipient resolution order**: (a) event's `NotifyType` opt-in devices of the target user → (b) all target user `AuthType=3` devices → (c) `RecipientList="staffonly"` single row (today's fallback), always recording history. The target user is the job owner = `JobOrder.CreatedBy` resolved to `User`, falling back to string-matching `JobOrder.OrderedBy` against `vwUserList_Active.UserAlias`/`UserName`, for every event including COGS. Rationale: matches legacy data (one owner GUID repeated per device) and the existing `ResolveUserGuidAsync` pattern in `EfJobManagementRepository`. When `CreatedBy == Guid.Empty` or unresolved (direct-SQL/imported orders), the `OrderedBy` fallback keeps rows flowing.

4. **`FcmEventHelperService` delegates to the shared publisher.** `IFcmEventHelperService.NotifyReadyPaperAsync/NotifyReadyPlateAsync` implementations call `JobLifecycleEventPublisher` for their event; `ScheduleCompatibilityController` is unchanged, and `ScheduleCompatibilityControllerTests` assertions are updated (`Topic` becomes `Device`). `FCM/SendMessage/*` fallback endpoints in `FcmCompatibilityController` keep their current `staffonly` behavior (manual replay, out of scope). Rationale: minimal churn to the ready path while unifying row composition. 

5. **Hook placement**: created → inside `EfJobManagementRepository.CreateJobOrder` after the first `SaveChangesAsync`; scheduled → in `SaveBatch` per `ScheduledItems` item right after the schedule upsert; completed → in `SaveBatch` completed block after `JobOrder.Status=2` + `SaveChangesAsync`, **and** in `EfJobManagementRepository.UpdateJobOrder` on the not-completed → completed transition; invoiced → in `EfJobManagementRepository.UpdateJobOrder` when `InvoiceRef`/`InvoiceAmount` transition from empty to set (job order form save), **and** on the `BillingService` mark-sent persist path that writes `InvoiceRef`/`InvoiceAmount` to the job (deduped by the same empty→set transition check, so the two paths cannot double-record for the same job); COGS → in `EfJobManagementRepository.CreateJobOrder` when `OriginalSONumber` is set, **and** in `EfJobManagementRepository.UpdateJobOrder` when `OriginalSONumber` changes to a set value (compare prior value), after `SaveChangesAsync`. The publisher is injected into the repository, controllers, and billing service (threaded through existing constructors; `EfJobManagementRepository` gets a constructor parameter using the existing DI style). Rationale: repository-level hooks cover both `JobOrdersController.Create` and `JobsController.Create`; transition detection (compare prior `Status`/`CompletedOn`/`InvoiceRef`/`OriginalSONumber` to new value) prevents double records when completion, invoicing, or COGS filling is reachable via two paths.

6. **Dead-store check for scheduled**: emit `OnJobScheduled` only for `ScheduledItems` (moved to "Selected Job Orders"), never from the `CompletedOrderIds`/`CancelledOrderIds` blocks of the same request.

7. **Legacy titles**: `JB5 新增訂單` (created), `JB5 已排單` (scheduled), `JB5 有鋅` (plate), `JB5 有紙` (paper), `JB5 全單完成` (completed), `JB5 已開發票` (invoiced), `JB5 已填成本` (COGS) — kept in a static map keyed by `JobLifecycleEventType`.

8. **Job-scoped COGS composition**: the COGS event builds its row from the `JobOrder`, using the same order body (`"{OrderNumber}-{JobNumber}: {CustomerName}"`) and owner targeting as the other events, because the COGS value lives on the job (`JobOrder.OriginalSONumber`, the `'COGS'` field in the job order form). Its webhook payload carries the order id. Notify type 16 is added to `UserCompatibilityController.MapNotifyType` for opt-in, alongside 15 for invoiced.

## Risks / Trade-offs

[Risk] Double webhook/history if completion is set through both `SaveBatch` and `UpdateJobOrder` in the same flow → Mitigation: completion events are guarded by a not-completed → completed transition check; each path checks the pre-update state. Same guard applies to invoicing: the `InvoiceRef`/`InvoiceAmount` empty→set transition is checked in both the job-form update and the mark-sent persist paths so they cannot double-record.

[Risk] `Notification`-only rows for jobs owned by unresolved users (`CreatedBy` empty, `OrderedBy` not matching a user) — history still recorded but recipients empty → falls back to `staffonly` row, preserving visibility.

[Risk] The COGS field (`OriginalSONumber`) is a free-text cost value on the job, not a numeric column with a fill guard → Mitigation: the COGS event fires when the value transitions to a non-empty value in `CreateJobOrder`/`UpdateJobOrder`, matching the legacy "job cost filled" semantics; clearing the field does not emit. If the stock `Product.COGS` (a separate product cost column) later needs its own event, it can be added independently.

[Risk] `HttpClient` in `JB2026.Api` default `AddHttpClient` without a timeout could hang request threads → Mitigation: register the named client with a short timeout (reuse Rest's 5s convention).

[Risk] README/test expectations reference the old `Topic="OnReadyPaper"` rows → Mitigation: update `ScheduleCompatibilityControllerTests` and search the repo for topic-value dependencies as part of tasks.

[Risk] Webhook event-name selection (`OnJobCreated`/`OnJobScheduled`/`OnJobCompleted`) is new to existing subscriptions → Mitigation: subscribers opt in via `EventTypes`; no default subscription is created, so no breaking change.

## Migration Plan

- Deploy order: ship `JB2026.Api` (production) with the created/scheduled/completed hooks and the synchronous dispatcher, and `JB2026.Rest` with the shared-publisher ready path. Both projects build and deploy together from the same repository; no DB migration, no config change.
- Rollback: revert the hook calls and `FcmEventHelperService` delegation; previously written `Device` rows are additive history and harmless to retain.
- Observability: rows are visible via `GET api/FCMHistory` to the owner (`UserIdList` match) — verify one record appears per event after deploy.

## Open Questions

- None for implementation. (Exact legacy title strings for created/scheduled events are an assumption — `JB5 新增訂單`/`JB5 已排單` — captured as an assumption in the spec and adjustable later without changing the approach.) Notify-type numbers 15/16 for invoiced/COGS are new assignments following the existing 10–14 range.
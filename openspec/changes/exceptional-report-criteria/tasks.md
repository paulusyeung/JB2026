## 1. Backend: expose schedule data

- [x] 1.1 Add `DateTime? ScheduledOn` and `bool HasActiveSchedule` to `JobOrderResponse` and verify `dotnet build` succeeds
- [x] 1.2 Populate the new fields in `EfJobManagementRepository.MapOrder(JobOrder)` from `job.JobSchedules` (earliest schedule with `Cancelled != true`) and verify the `JobOrdersController` job-list response includes them (unit test or manual curl)
- [x] 1.3 Mirror the fields in `InMemoryJobManagementRepository.MapOrder` and verify `dotnet test JB2026.Api.ParityTests` passes

## 2. Frontend: persistence plumbing

- [x] 2.1 Extend `ViewSettings` in `src/composables/useColumnPersistence.ts` with an optional `criteria?: Record<string, { enabled: boolean; days: number }>` (defaults + parse + apply + save) and verify `npm run typecheck` passes
- [x] 2.2 Verify existing views' settings still round-trip through localStorage and server preference (manual check of two other views, or existing e2e suite)

## 3. Frontend: criteria engine in ExceptionalReportView

- [x] 3.1 Define the criteria config array (`id`, label key, default `days`, `enabled`, `test(row, today)`) for: not scheduled, not completed, no invoice, no invoice after completed, no COGS; treat 1900-01-01 / missing dates as absent; verify predicates via a unit test or typecheck
- [x] 3.2 Add `filteredRows` computed applying OR across enabled criteria, and switch `sortedRows`, summary chips, and row counts to operate on it; verify the table shows only matching rows in a running dev server
- [x] 3.3 Persist criteria state under the `exceptional-report` view id via `useViewSettings`; verify toggles/thresholds survive reload
- [x] 3.4 Add the criteria panel UI (toolbar popover: switch + day stepper per criterion) and verify it opens, toggles, and updates thresholds from the UI
- [x] 3.5 Add the matched-reasons column (`exceptionalReasons`) rendered as chips with i18n labels, toggleable via the column picker; verify it shows reason chips for a matching row
- [x] 3.6 Add i18n keys (`reports.exceptional.criteria.*`) in en/zhHans/zhHant and verify no missing-key warning for these labels

## 4. Tests and verification

- [x] 4.1 Update `tests/smoke.spec.ts` exceptional-report test for the date-range UI and criteria behavior (assert a criterion match is visible), and verify `npm run test:smoke` passes
- [x] 4.2 Run `npm run lint` and `npm run typecheck` in ClientApp; verify clean
- [x] 4.3 Manual end-to-end: enable criteria with a low threshold, confirm rows appear and reasons are shown; raise threshold, confirm rows drop without a server reload (watch network tab for no new `/api/v2/job-orders` calls on threshold change)
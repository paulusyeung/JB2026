# Tasks — mobile-ux-transformation

## 1. Foundation

- [ ] 1.1 Fix `src/composables/useTouch.ts` (valid TS module, no string-wrapped source); expose `isTouchDevice` and safe-area helper; avoid duplicate resize listeners if used from multiple components
- [ ] 1.2 Add shared CSS or utility for `env(safe-area-inset-bottom)` on bottom sheets and fixed footers
- [ ] 1.3 Create `src/components/scheduler/JobActionMenu.vue` with machine targets and move actions; min 44px tap targets; emit selected machine/action
- [ ] 1.4 Extend `ListMobileCard.vue` if needed for schedule use (checkbox multi-select, `#actions` slot); define `scheduleAvailableColumns` / `scheduleScheduledColumns` column configs
- [ ] 1.5 Remove or archive unused `AdaptiveRow.vue` after confirming zero imports

## 2. Schedule board — mobile workflow

- [ ] 2.1 Refactor `ScheduleView.vue`: split render trees — `ListMobileCard` + bottom sheet on `isPhoneLayout`, existing tables on desktop
- [ ] 2.2 Implement "Add jobs" FAB/button and `v-bottom-sheet` for Available list with selection and select-all
- [ ] 2.3 Integrate `JobActionMenu` in sheet for M1–M5 transfer; remove vertical transfer column on phone
- [ ] 2.4 Show print Qty, Color, Size on scheduled mobile cards; remove `v-if="!isPhoneLayout"` suppressions for those fields on phone
- [ ] 2.5 Remove desktop-preferred alert from `ScheduleView.vue` on phone; add i18n keys for new strings (`scheduler.schedule.addJobs`, etc.)
- [ ] 2.6 Implement local transfer feedback: update Available/Scheduled arrays on move; snapshot at Save; rollback or reload on `saveScheduleBatch` failure with user-visible error

## 3. Pivot analytics and calendar

- [ ] 3.1 `JobStatsView.vue`: wire summary tiles to filter/drill actions; remove `mobilePreferredNotice` on phone when summary is actionable
- [ ] 3.2 `SmlInvoiceStatsView.vue`: same summary interaction pattern; remove phone notice
- [ ] 3.3 `SmlRtfStatsView.vue`: align with summary + notice removal if view has pivot/summary pattern
- [ ] 3.4 `SchedulerView.vue`: validate simplified mobile calendar; remove desktop-preferred notice only if operable; otherwise document limitation in `MOBILE_LIMITATIONS.md`

## 4. Documentation, i18n, and regression

- [ ] 4.1 Update `MOBILE_LIMITATIONS.md` — scheduler board no longer desktop-preferred; note any remaining FullCalendar limitations
- [ ] 4.2 Remove unused `mobilePreferredNotice` i18n keys per locale where notices are removed
- [ ] 4.3 Extend `tests/responsive.mobile.spec.ts`: schedule sheet → select → transfer → verify scheduled; no desktop-preferred notice; print fields visible
- [ ] 4.4 Manual QA matrix: 360px, 390px, 430px on schedule board move-job flow and one pivot screen

## Recommended order

| Order | Tasks | Depends on |
|-------|-------|------------|
| 1 | 1.1–1.4 | — |
| 2 | 2.1–2.6 | 1.x |
| 3 | 3.1–3.4 | — (parallel after 2.1 pattern clear) |
| 4 | 4.1–4.4 | 2.x, 3.x |

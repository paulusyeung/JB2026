# Proposal — mobile-ux-transformation

## Why

The `clientapp-mobile-readiness` change delivered Tier 1 list/card patterns and Tier 3 *containment* for scheduler and pivot screens (stacked panels, reduced columns, desktop-preferred notices). Operators on phones still face high friction on the scheduler board: vertical scroll between Available and Scheduled, hidden print fields (Qty, Color, Size), dense M1–M5 transfer buttons, and warnings that signal "not really supported" rather than offering a real workflow.

This change upgrades Tier 3 from **layout adaptation** to **adaptive workflows**—different interaction models on narrow viewports while preserving the existing desktop board. It builds on established patterns (`ListMobileCard`, `useDisplay`, pivot summary cards) instead of introducing parallel abstractions.

## What Changes

- **Schedule board (`ScheduleView.vue`)**: Scheduled-first layout on phone; Available jobs moved into a bottom sheet; machine transfer via touch-friendly action menu; all critical scheduled columns visible on mobile (including Qty, Color, Size).
- **Schedule list presentation**: Reuse/extend `ListMobileCard` for Available and Scheduled rows on mobile; retire or repurpose stub `AdaptiveRow.vue` unless a concrete desktop `<tr>` slot need remains.
- **`useTouch` composable**: Fix and scope for safe-area padding and touch affordances only; **layout breakpoints remain `useDisplay`**.
- **Local transfer feedback**: Immediate UI update when moving jobs between Available/Scheduled before explicit Save; rollback on Save failure (not optimistic server round-trip on Save).
- **Pivot analytics**: Make existing summary cards actionable (filter/drill entry points); remove desktop-preferred notices where summary + contained pivot is sufficient.
- **FullCalendar scheduler (`SchedulerView.vue`)**: Improve narrow-phone calendar defaults; remove desktop-preferred banner only if simplified view is genuinely usable (separate success bar from schedule board).
- **Global**: Audit and remove `mobilePreferredNotice` / hardcoded alerts on routes where mobile UX meets acceptance criteria; update `MOBILE_LIMITATIONS.md`.
- **Tests**: Extend `responsive.mobile.spec.ts` with scheduler sheet → select → transfer flow.

No backend API or contract changes. No breaking changes to desktop scheduler behavior.

## Capabilities

### New Capabilities

- `scheduler-mobile-workflow`: Adaptive scheduler board workflow (bottom sheet, action menu, scheduled-first, column visibility, local transfer feedback).
- `schedule-mobile-list-cards`: Mobile card presentation for Available/Scheduled lists using the shared list-card pattern.
- `pivot-analytics-mobile-summary`: Actionable summary-first pivot entry on phone viewports.
- `mobile-touch-affordances`: Touch/safe-area utilities scoped to affordances, not layout switching.
- `mobile-scheduler-regression`: Playwright coverage for scheduler mobile workflow.

### Modified Capabilities

- None (no published requirements in `openspec/specs/`; this change supersedes the Tier 3 *intent* documented in `clientapp-mobile-readiness` / `MOBILE_LIMITATIONS.md` for scheduler and pivot routes).

## Impact

- **Users**: Usable move-job workflow on phone without cross-viewport scrolling; pivot screens useful without dismissing the app.
- **Frontend**: `ScheduleView.vue`, new `JobActionMenu.vue`, `useTouch.ts`, schedule mobile list integration, `JobStatsView.vue`, `SmlInvoiceStatsView.vue`, `SmlRtfStatsView.vue`, `SchedulerView.vue`, i18n keys, `MOBILE_LIMITATIONS.md`.
- **Testing**: `tests/responsive.mobile.spec.ts` (+ optional component tests).
- **Out of scope**: Backend APIs, Pinia store introduction solely for this change, full FullCalendar feature parity on phone.

# Mobile Readiness Notes and Desktop-Preferred Workflows

This document summarizes mobile behavior for the ClientApp and calls out workflows that are intentionally desktop-preferred.

## Desktop-Preferred Workflows

### 1) Scheduler board (ScheduleView.vue)

- Route: /app/job-order/schedule/scheduled
- Reason: This workflow uses multi-panel scheduling interactions (available jobs, transfer actions, and scheduled jobs) that are most efficient with desktop width and pointer precision.
- Mobile treatment:
  - Layout is stacked vertically instead of side-by-side.
  - Transfer controls are condensed and scrollable.
  - Non-essential columns are reduced.
  - A desktop-preferred notice is shown on narrow phones.

### 2) FullCalendar scheduler (SchedulerView.vue)

- Route: /app/scheduler
- Reason: Dense calendar controls and scheduling context are significantly easier to operate on desktop.
- Mobile treatment:
  - Calendar defaults to a simplified mobile view.
  - Non-essential controls are hidden or reduced.
  - A desktop-preferred notice is shown on narrow phones.

### 3) Pivot analytics screens

- Routes:
  - /app/job-order/sml/invoice-stats
  - /app/job-order/job-stats
  - /app/job-order/sml/rtf-stats
- Reason: Pivot exploration, multi-dimensional slicing, and wide table interpretation are desktop-optimized tasks.
- Mobile treatment:
  - Compact summary cards are shown before pivot content.
  - Pivot containers are constrained with horizontal scrolling.
  - A desktop-preferred notice is shown for analysis-heavy views.

## Responsive Patterns by Tier

### Tier 1 (Highest Value / Lower Risk)

- Scope:
  - Job and order list views
  - Admin CRUD list views
- Pattern:
  - Switch dense desktop tables to mobile card layouts on phone breakpoints.
  - Collapse secondary toolbar actions into overflow menus.
  - Keep filter areas stacked/collapsible on smaller widths.
  - Ensure dialogs are scrollable and use viewport-safe max widths.

### Tier 2 (Secondary Screens)

- Scope:
  - Quotations, reports, settings, help, public, dashboard
- Pattern:
  - Preserve desktop structure where low density already works.
  - Add card/list treatment only where data density requires it.
  - Keep theme readability and contrast in both light and dark modes.

### Tier 3 (Exception / Complex Workflows)

- Scope:
  - Scheduling and pivot analytics
- Pattern:
  - Prioritize operability over parity for narrow viewports.
  - Use simplified views, reduced control density, and summary-first layouts.
  - Explicitly communicate desktop preference for high-complexity tasks.

## Validation Status

- Automated mobile viewport matrix checks were added in responsive.mobile.spec.ts for Tier 1 and Tier 3 route coverage.
- Desktop smoke regression checks are tracked separately in smoke.spec.ts and must pass before rollout completion.

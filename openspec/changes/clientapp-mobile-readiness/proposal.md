# Proposal — clientapp-mobile-readiness

## Why

The Vue 3 ClientApp delivered in Phase 6 is functional on desktop, but it is not consistently usable on phones or small tablets. The shared shell keeps a permanent navigation drawer, top-level actions compete for horizontal space, and many views rely on dense data tables with explicit column widths. Existing breakpoint handling mostly collapses filter bars and spacing, but does not provide a mobile interaction model for navigation, grids, schedulers, calendars, or analytics views.

Without an explicit mobile-readiness pass, the SPA risks poor usability for supervisors, sales staff, and operators who need quick access away from a desktop workstation. The current Playwright coverage also validates desktop only, so responsive regressions could be introduced without detection.

## What Changes

Create a dedicated mobile-readiness retrofit for `JB2026.WebApp/ClientApp` with a phased implementation plan. The work introduces a responsive app shell, standard mobile patterns for filter/tooling bars, adaptive behavior for data-heavy list views, explicit exception handling for scheduler and pivot/calendar screens, and mobile viewport test coverage.

The proposal does not assume that every desktop table can simply shrink to phone width. Instead, it establishes per-screen patterns: mobile drawers, stacked controls, horizontal-scroll containment where appropriate, and alternate list/card presentations where dense tables become unusable.

## Assessment Summary

- The current stack supports a retrofit well: Vue 3, Vuetify 3, scoped component styles, and Playwright are already in place.
- The main risk is breadth, not technical feasibility. Multiple views repeat the same desktop-first patterns.
- Shared shell issues are concentrated in the permanent navigation drawer and crowded top bar.
- Data-heavy views such as stock, orders, jobs, admin grids, and schedule boards require view-specific mobile patterns.
- FullCalendar and pivot-table views are higher-risk because their usable mobile layouts are constrained by third-party widgets.
- Existing end-to-end coverage targets desktop only, so mobile verification must be added as part of the change.

## Capabilities

- `responsive-app-shell` — mobile drawer, adaptive topbar, consistent spacing, and viewport-aware shell behavior
- `responsive-data-list-patterns` — standard responsive treatment for stock, order, job, and admin list screens
- `mobile-exception-layouts` — dedicated mobile strategies for scheduler, calendar, and pivot-style analytics screens
- `mobile-regression-coverage` — Playwright mobile viewport coverage and responsive acceptance checks
- `responsive-rollout-governance` — phased delivery with clear risk tiers and validation gates

## Impact

- **Users** — Improved usability on phones and tablets; desktop workflows remain primary for dense operational tasks
- **Frontend** — Shared shell and view-level layout refactors across multiple Vue SFCs
- **Testing** — Playwright configuration expands to include mobile viewport validation
- **Delivery** — Best executed as a phased retrofit rather than a single bulk rewrite
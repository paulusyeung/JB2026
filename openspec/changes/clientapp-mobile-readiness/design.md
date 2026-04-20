# Design — clientapp-mobile-readiness

## Context

Phase 6 replaced the legacy WebForms UI with a Vue 3 SPA built on Vuetify 3, FullCalendar, Chart.js, and a web pivot table component for analytics-heavy screens. The result is modernized, but most workflows remain desktop-first. Representative issues already visible in the codebase include:

- Permanent navigation drawer in the shared shell
- Dense topbar controls with limited small-screen adaptation
- Repeated `v-data-table` screens with fixed or minimum-width columns
- Custom schedule boards built with wide tables and resizable columns
- FullCalendar weekly view as the default calendar presentation
- Pivot-style analytics screens that assume large horizontal space

The design goal is not to force all workflows into phone parity. It is to make the app meaningfully usable on smaller screens, while preserving desktop productivity for dense operational screens.

## Goals

- Establish a consistent responsive shell across all authenticated ClientApp routes
- Define a repeatable mobile treatment for filter bars, toolbars, dialogs, and data list views
- Reduce horizontal overflow and control collisions on common screens
- Add explicit responsive strategies for scheduler, calendar, and pivot-style analytics pages
- Add mobile viewport test coverage to catch regressions early

## Non-Goals

- Full UX redesign of the Phase 6 application
- Replacing Vuetify, FullCalendar, or the pivot-table library
- Guaranteeing feature parity for every dense desktop workflow on a narrow phone viewport
- Reworking backend APIs or data contracts purely for responsive styling needs

## Decisions

### D1: Treat mobile readiness as a phased retrofit, not a single CSS pass

The codebase contains repeated desktop-first patterns across many views. A shell-only fix would not make the app mobile-ready. Delivery is split into shell, common list screens, exception screens, and mobile verification.

### D2: Use viewport-aware behavior in the shared shell

The shared app shell should adopt Vuetify display breakpoints and switch from a permanent drawer to a temporary or overlay drawer on smaller screens. Topbar controls should stack, collapse, or move into menus instead of relying on one crowded row.

### D3: Use standard responsive patterns for data-heavy screens

Data list screens should use one of three patterns based on density:

- horizontal-scroll table with protected wrapper for medium-density screens
- reduced-column table with column presets for small screens
- alternate stacked list or card presentation for screens where wide grids become unreadable

Each screen should explicitly choose a pattern rather than relying on automatic squeezing.

### D4: Treat scheduler and analytics screens as exception workflows

Schedule boards, FullCalendar views, and pivot-table analytics are not ordinary grid screens. They require tailored mobile behavior such as simplified default views, reduced controls, read-only summaries, or explicit desktop-preferred messaging when a workflow is not practical on a narrow phone screen.

### D5: Add responsive test coverage before broad rollout

Mobile viewport coverage must be part of the implementation, not a later polish task. Playwright should add at least one phone-sized project and cover navigation, top-level lists, and critical workflow entry points.

## Risk Assessment

| ID | Risk | Assessment | Mitigation |
|----|------|------------|------------|
| MR-1 | Scope spreads across too many views | High | Deliver in phases with a shared responsive pattern library and priority tiers |
| MR-2 | Dense tables become unusable on phones | High | Define per-screen mobile patterns instead of forcing table shrinkage |
| MR-3 | Scheduler boards lose critical functionality on small screens | High | Provide dedicated mobile handling and accept desktop-preferred fallbacks where necessary |
| MR-4 | Third-party widgets constrain layout behavior | Medium | Prototype FullCalendar and pivot layouts early before large-scale view refactors |
| MR-5 | Desktop regressions introduced while improving mobile | Medium | Keep desktop layout as baseline, use targeted breakpoints, and add viewport-specific smoke coverage |
| MR-6 | Team underestimates verification effort | Medium | Expand Playwright and include a manual QA matrix for high-density views |

## Implementation Plan

### Phase 1: Shared Shell and Responsive Foundations

- Introduce breakpoint-aware shell behavior using Vuetify display utilities
- Convert the sidebar into a mobile drawer pattern
- Rework topbar controls so they wrap or collapse predictably
- Add shared utility classes or composables for responsive spacing, scroll containers, and toolbar layout

### Phase 2: Standard List and Form Screens

- Retrofit stock, jobs, orders, quotations, and admin list screens to use approved responsive patterns
- Normalize mobile behavior for filter bars, action groups, and dialogs
- Prefer limited-column mobile defaults over full-width table compression when density is too high

### Phase 3: Exception Screens

- Rework schedule views and packing/scheduling boards with explicit small-screen behavior
- Evaluate FullCalendar small-screen defaults such as alternate initial views or reduced controls
- Audit pivot-table analytics screens and determine whether mobile should use contained horizontal scrolling, summary-first layouts, or desktop-preferred guidance

### Phase 4: Mobile Verification and Hardening

- Add Playwright phone viewport projects and smoke flows
- Validate navigation, login/session bootstrap, key list screens, and at least one schedule/calendar path on mobile
- Run manual QA against high-risk views with an agreed device-width matrix

## Prioritization

### Tier 1: Highest Value / Lowest-to-Medium Risk

- App shell
- Topbar
- Sidebar
- Stock list
- Job list
- Order list
- Admin CRUD list views

### Tier 2: Medium Complexity

- Forms and dialogs
- Quotations and reports
- Public/help/settings views

### Tier 3: Highest Complexity / Highest Risk

- Schedule views
- SchedulerView FullCalendar screens
- Pivot analytics screens

## Open Questions

- Q1: What device widths define supported mobile and tablet targets for this product?
- Q2: Should certain operational screens be marked as desktop-preferred instead of fully optimized for narrow phones?
- Q3: Is the expected outcome “usable on phone” or “first-class mobile workflow” for scheduling and analytics screens?
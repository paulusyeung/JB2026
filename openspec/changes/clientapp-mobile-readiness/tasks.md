# Tasks — clientapp-mobile-readiness

## Group 1: Discovery and Responsive Baseline

- [x] Finalize supported viewport matrix for phone and tablet breakpoints
- [x] Catalog ClientApp routes by responsive complexity: standard, medium, exception
- [x] Define shared responsive patterns for shell, toolbars, filter bars, data lists, and dialogs

## Group 2: Shared Shell

- [x] Refactor the authenticated shell to use breakpoint-aware drawer behavior
- [x] Rework the topbar so language, theme, profile, and sign-out controls remain usable on narrow widths
- [x] Add shared responsive spacing and scroll-container rules in the global stylesheet

## Group 3: Standard Data Screens

### Tier 1: Highest Value / Lowest-to-Medium Risk

- [x] **Task 1.1: Retrofit `JobListView.vue` with Mobile Card Pattern** (2–3h)
  - Add `useDisplay` composable and `isPhoneLayout` computed (`display.smAndDown`)
  - Add mobile card rendering alongside existing `v-data-table`:
    - Card header: order type icon + order number + customer name
    - Card body: status chip, order title, product style, invoice amount
    - Card footer: orderedOn, requiredOn, modifiedBy
    - Card actions: "More" menu with checkbox, popup, print, export, newOrder
  - Collapse toolbar actions into a "More" overflow menu on mobile (`v-menu`)
  - Filter bar already collapses at 960px — keep as-is
  - Dialog already has `max-width="760"` — add `max-width="min(100%, 760px)"` for mobile
  - Add mobile-specific CSS: `.job-mobile-card`, `.job-mobile-card__header`, `__body`, `__footer`
  - Pattern to follow: `StockView.vue` mobile card implementation

- [x] **Task 1.2: Retrofit `OrderListView.vue` with Mobile Card Pattern** (2.5–3.5h)
  - Same approach as Task 1.1 — add `isPhoneLayout` computed
  - Mobile card rendering:
    - Master row card: order number, customer, brand, requiredOn, invoiceAmount
    - Detail rows: collapse into an expandable section within the card (or show on card tap)
    - Status chip, orderedBy, orderedOn
  - Toolbar actions collapse into "More" menu on mobile
  - Dialog already has `max-width="1080"` — add mobile width limit
  - Filter bar already collapses at 960px
  - Complexity note: Master-detail rows — show master card with expand/collapse chevron for detail rows

- [x] **Task 1.3: Retrofit Admin CRUD Views (Batch Pattern)** (4–6h)
  - Create shared composable `useResponsiveList()` providing:
    - `isPhoneLayout` computed from `display.smAndDown`
    - `isTabletLayout` computed from `display.mdAndDown`
    - Responsive column visibility helper
  - Create shared mobile card component `ListMobileCard.vue`:
    - Accepts `items`, `columns`, `checkboxMode`, `onSelect` props
    - Renders a card per item with configurable fields
    - Includes a "More" menu slot for actions
  - Apply pattern to all 8 admin views:
    - `AdminUserView.vue`
    - `AdminCustomerView.vue`
    - `AdminSupplierView.vue`
    - `AdminWorkflowView.vue`
    - `AdminWorkflowFormsView.vue`
    - `AdminOrderTypeView.vue`
    - `AdminQuotationItemGroupView.vue`
    - `AdminQuotationItemView.vue`
  - Add `isPhoneLayout` computed, swap `v-data-table` for `ListMobileCard` on mobile
  - Collapse toolbar into "More" menu on mobile
  - Filter bar already collapses at 960px in most views
  - Dialogs already have `max-width` — ensure they degrade to `min(100%, 760px)`

- [x] **Task 1.4: Audit and Fix Dialogs from List Screens** (2–3h)
  - Audit `JobOrderForm.vue`, `OrderRecordDialog.vue`, `AdminUserRecordDialog.vue`, and similar form dialogs
  - Ensure all dialogs use responsive width: `max-width="min(100%, 760px)"` or similar
  - Ensure `scrollable` is present on all dialogs
  - Ensure form fields stack vertically on mobile (use Vuetify `v-row`/`v-col` or `flex-column`)
  - Ensure action buttons stack or wrap on mobile

## Group 4: Secondary Screens

### Tier 2: Medium Complexity

- [x] **Task 2.1: Audit and Retrofit Quotations, Reports, Settings, Help, Public Views** (3–4h)
  - Audit each view for:
    - Dense tables → apply mobile card pattern where needed
    - Filter bars → ensure stacking at tablet/phone widths
    - Toolbars → collapse secondary actions into menus
    - Dialogs → responsive width limits
  - Views to audit: `QuotationsView.vue`, `ReportsView.vue`, `SettingsView.vue`, `HelpView.vue`, `PublicView.vue`, `DashboardView.vue`
  - `DashboardView.vue` — likely already responsive (uses cards/grid)
  - `SettingsView.vue`, `HelpView.vue`, `PublicView.vue` — likely low-density, minimal changes
  - `QuotationsView.vue`, `ReportsView.vue` — may have dense tables, apply pattern

- [x] **Task 2.2: Validate Dark/Light Theme After Responsive Changes** (1–2h)
  - Manual visual check of all Tier 1 and Tier 2 views in both themes
  - Verify card backgrounds, borders, and text contrast on mobile
  - Verify pivot table theme switching works (already partially done in SmlInvoiceStatsView)
  - Added automated mobile dark/light coverage in `tests/responsive.mobile.spec.ts` for Tier 2 routes and pivot visibility
  - Local execution currently blocked in this environment by missing Playwright system dependencies (`npx playwright install-deps`)

## Group 5: Exception Workflows

### Tier 3: Highest Complexity / Highest Risk

- [ ] **Task 3.1: Prototype Mobile Treatment for Schedule Views** (7–11h total)
  
  **3.1a: `ScheduleView.vue` (main scheduler board)** (4–6h)
  - On mobile: switch from 3-column horizontal layout to stacked vertical panels
  - Available panel on top, scheduled panel below
  - Transfer buttons become a horizontal scrollable strip between panels
  - Reduce column count on mobile (hide printQty, printColor, printSize, show only essential columns)
  - Machine filter becomes a horizontal scrollable chip group
  - Add a "Desktop preferred" notice banner at the top on narrow phones
  
  **3.1b: `SchedulePendingView.vue`, `ScheduleCompletedView.vue`** (1–2h each)
  - Likely table-based lists — apply mobile card pattern (same as Tier 1)
  
  **3.1c: `SchedulePackingView.vue`, `SchedulePackingOnAirView.vue`** (1–2h each)
  - Audit for dense tables — apply mobile card pattern

- [ ] **Task 3.2: Mobile Treatment for `SchedulerView.vue` (FullCalendar)** (2–3h)
  - On mobile (`smAndDown`):
    - Change `initialView` from `'timeGridWeek'` to `'dayGridMonth'` or `'listWeek'`
    - Hide non-essential toolbar controls
    - Reduce calendar height to fit viewport
  - On tablet (`mdAndDown`):
    - Keep `timeGridWeek` but reduce control density
  - Add a "Desktop preferred for scheduling" notice on narrow phones

- [ ] **Task 3.3: Audit Pivot Analytics Screens** (4–6h total)
  
  **3.3a: `SmlInvoiceStatsView.vue`** (2–3h)
  - On mobile: constrain pivot container height, enable horizontal scroll
  - Show a summary card above the pivot with key totals
  - Add "Desktop preferred for pivot analysis" notice
  
  **3.3b: `JobStatsView.vue`, `SmlRtfStatsView.vue`** (2–3h total)
  - Audit current implementation
  - Apply same pattern: summary cards + contained pivot/table

## Group 6: Mobile Test Coverage

- [x] Add Playwright mobile viewport project configuration alongside desktop coverage
- [x] Extend smoke tests to validate shell navigation and at least the stock, jobs, and orders entry paths on mobile
- [x] Add regression checks for drawer behavior, topbar controls, and responsive table/list rendering

## Group 7: Validation and Rollout

### Validation Tasks

- [ ] **Task 7.1: Manual QA Across Tier 1 and Tier 3 Views** (4–6h)
  - Test all Tier 1 views on: 360px, 390px, 430px, 768px, 834px, 1024px
  - Test all Tier 3 views on: 360px, 390px, 768px
  - Verify no horizontal overflow on any viewport
  - Verify drawer toggle, topbar, and navigation work correctly

- [ ] **Task 7.2: Confirm No Desktop Regressions** (1–2h)
  - Run existing Playwright desktop tests (`smoke.spec.ts`)
  - Verify all core list, dialog, and navigation flows still work on desktop
  - Verify desktop layout is unchanged (permanent drawer, full toolbar)

- [ ] **Task 7.3: Document Known Desktop-Preferred Workflows** (1h)
  - Create a `MOBILE_LIMITATIONS.md` file in the ClientApp directory
  - Document which workflows are desktop-preferred:
    - Scheduler board (`ScheduleView.vue`)
    - FullCalendar scheduler (`SchedulerView.vue`)
    - Pivot analytics screens
  - Document the responsive patterns used for each tier
  - Update this `tasks.md` with completion status

---

## Recommended Execution Order

| Day | Tasks | Effort |
|-----|-------|--------|
| 1 | Task 1.1 (JobListView) + Task 1.2 (OrderListView) | 4.5–6.5h |
| 2 | Task 1.3 (Admin CRUD batch) | 4–6h |
| 3 | Task 1.4 (Dialogs) + Task 2.1 (Secondary screens) | 5–7h |
| 4 | Task 3.1 (Schedule views) | 7–11h |
| 5 | Task 3.2 (FullCalendar) + Task 3.3 (Pivot) + Task 2.2 (Theme) | 7–11h |
| 6 | Task 7.1 (QA) + Task 7.2 (Regression) + Task 7.3 (Docs) | 6–9h |
| | **Total** | **~34–50h** |

## Summary Table

| Group | Task | Effort | Priority |
|-------|------|--------|----------|
| 1.1 | JobListView mobile cards | 2–3h | Tier 1 |
| 1.2 | OrderListView mobile cards | 2.5–3.5h | Tier 1 |
| 1.3 | Admin CRUD views (batch) | 4–6h | Tier 1 |
| 1.4 | Dialog/form responsive audit | 2–3h | Tier 1 |
| 2.1 | Quotations, Reports, Settings, Help, Public | 3–4h | Tier 2 |
| 2.2 | Dark/light theme validation | 1–2h | Tier 2 |
| 3.1 | Schedule views mobile treatment | 7–11h | Tier 3 |
| 3.2 | FullCalendar mobile treatment | 2–3h | Tier 3 |
| 3.3 | Pivot analytics mobile treatment | 4–6h | Tier 3 |
| 7.1 | Manual QA | 4–6h | Group 7 |
| 7.2 | Desktop regression tests | 1–2h | Group 7 |
| 7.3 | Documentation | 1h | Group 7 |
| | **Total** | **~34–50h** | |
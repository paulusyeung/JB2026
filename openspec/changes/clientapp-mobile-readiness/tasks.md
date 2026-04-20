# Tasks — clientapp-mobile-readiness

## Group 1: Discovery and Responsive Baseline

- [ ] Finalize supported viewport matrix for phone and tablet breakpoints
- [ ] Catalog ClientApp routes by responsive complexity: standard, medium, exception
- [ ] Define shared responsive patterns for shell, toolbars, filter bars, data lists, and dialogs

## Group 2: Shared Shell

- [ ] Refactor the authenticated shell to use breakpoint-aware drawer behavior
- [ ] Rework the topbar so language, theme, profile, and sign-out controls remain usable on narrow widths
- [ ] Add shared responsive spacing and scroll-container rules in the global stylesheet

## Group 3: Standard Data Screens

- [ ] Retrofit `StockView.vue` with a mobile-safe filter and data presentation
- [ ] Retrofit `JobListView.vue` and `OrderListView.vue` with mobile-safe toolbars and list/table behavior
- [ ] Retrofit admin CRUD views (`AdminUserView.vue`, `AdminCustomerView.vue`, `AdminSupplierView.vue`, workflow and quotation admin screens) using the same responsive pattern set
- [ ] Audit dialogs and forms opened from list screens for narrow-width usability

## Group 4: Secondary Screens

- [ ] Audit and retrofit quotations, reports, settings, help, and public-content views
- [ ] Validate dark/light theme behavior after responsive layout changes

## Group 5: Exception Workflows

- [ ] Prototype responsive behavior for `ScheduleView.vue`, `SchedulePendingView.vue`, `ScheduleCompletedView.vue`, `SchedulePackingView.vue`, and `SchedulePackingOnAirView.vue`
- [ ] Decide mobile treatment for `SchedulerView.vue` and implement the chosen FullCalendar small-screen strategy
- [ ] Audit `JobStatsView.vue`, `SmlRtfStatsView.vue`, and `SmlInvoiceStatsView.vue` and implement either responsive containment or desktop-preferred fallbacks

## Group 6: Mobile Test Coverage

- [ ] Add Playwright mobile viewport project configuration alongside desktop coverage
- [ ] Extend smoke tests to validate shell navigation and at least the stock, jobs, and orders entry paths on mobile
- [ ] Add regression checks for drawer behavior, topbar controls, and responsive table/list rendering

## Group 7: Validation and Rollout

- [ ] Run manual QA across Tier 1 and Tier 3 views using the agreed viewport matrix
- [ ] Confirm no desktop regressions in core list, dialog, and navigation flows
- [ ] Document known desktop-preferred workflows, if any remain after implementation
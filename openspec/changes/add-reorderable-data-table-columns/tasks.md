## 1. Scope Guard

- [x] 1.1 Keep the change to the 16 in-scope views and the four views with icon header cells; do not convert unused `SmlRtfListView`/`SmlInvoiceListView`/`QuotationsView`, menu-less `AdminView`/`JobOrderView`/`PublicView`/`SmlView`, or non-persistent `AdminWorkflowView`/`AdminWorkflowFormsView`/`AdminQuotationItemView`/`AdminQuotationItemGroupView`. `OrderListView` and the six `CrmCustomer360View` tables were originally listed here; both are in scope now (sections 6 and 7)

## 2. Shared Infrastructure

- [x] 2.1 Create `useColumnOrder(columnOrder, allHeaders, defaultKeys)` returning `orderedHeaders`, `orderedColumnKeys`, `moveColumn(sourceKey, targetKey)` and `resetColumnOrder`; verify unknown keys are ignored, missing keys appended in default order, and a move is a no-op when source equals target
- [x] 2.2 Create `src/components/grids/ReorderableTableHeaders.vue` with props `table` (the `#headers` slot-props object), `headerExtras` (`Record<key, { icon, size?, color?, title?, align? }>`), `reorderableKeys?` and `reorderable?` (default `true`), emitting `move`; verify it renders the row, owns the drag state, and carries the drag-affordance styles scoped to itself
- [x] 2.3 In the component, reproduce the prototype's cell rendering exactly: `v-data-table__td v-data-table__th v-data-table__th--sticky`, `v-data-table-column--align-*`, `v-data-table-column--no-padding` on `data-table-select`, the `v-data-table-header__content` wrapper, `v-data-table-header__sort-icon`, the inline sticky style, `toCssUnit` widths, click/Enter sort, and the select-all checkbox; verify against the recorded baseline in 8.1
- [x] 2.4 Refactor JobListView onto the composable and the component: delete the inline header markup, drag handlers and drag CSS, and pass `headerExtras` for its icon columns; verify `tests/job-list.column-order.spec.ts` passes, with its header-cell locator updated from the view-owned `.job-list-th` class to the component-owned `.reorderable-th` (see design.md - Decision 1)
- [x] 2.5 Run the component spec (8.1) and the job-list spec before converting any other view, and treat a failure here as a stop rather than a per-view workaround

## 3. Preference Key Registration

- [x] 3.1 Add GUID entries to `viewPreferenceKeys.ts` for the 13 in-scope view ids that lack one: crm-opportunities, staff-members, crm-tasks, admin-user, admin-customer, admin-supplier, billing-clients, billing-invoices, billing-statement, pending-schedule, completed-schedule, packing-schedule, exceptional-report (stock, crm-people and crm-companies are already registered)
- [x] 3.2 Verify a registered view writes a preference row on first settings change and restores it on the next load
- [x] 3.3 Verify a view without a registered id still works localStorage-only (no failed requests, no console errors)
- [x] 3.4 Add one GUID per `CrmCustomer360View` table (6 entries: crm-customer360-job-orders, -invoices, -opportunities, -tasks, -files, -emails), so the six tables each persist their own order rather than sharing the view's

## 4. Rollout: Simple Views

For each view: add the `#headers` slot wired to `ReorderableTableHeaders`, replace any icon header slot with a `headerExtras` entry, call `useColumnOrder` with the view's `allHeaders` and default key list, add `columnOrder` to its `useViewSettings` defaults, add the reset item plus its own `resetColumns` key to the Columns menu, and register the object id from group 3 in the same change. Keep the view's own table class and `:deep(.v-data-table__th)` rules.

- [x] 4.1 StockView (replace `header.attachment` icon slot with `headerExtras`, adding a title it currently lacks) and verify drag reorder, persistence, menu order, reset, sorting, checkbox mode and the icon header cell
- [x] 4.2 CrmPeopleView (replace `header.synced` with `headerExtras`) and verify the indicator column, phone/tablet column hiding and preference round-trip
- [x] 4.3 CrmCompaniesView (replace `header.synced` with `headerExtras`) and verify phone/tablet column hiding and preference round-trip
- [x] 4.4 CrmOpportunitiesView and CrmStaffMembersView and verify card view, sorting and Columns menu
- [x] 4.5 CrmTasksView and verify card view, checkbox mode and Columns menu
- [x] 4.6 AdminUserView, AdminCustomerView and AdminSupplierView and verify Columns menu, checkbox mode and preference round-trip
- [x] 4.7 BillingClientsView, BillingInvoicesView and BillingStatementView and verify Columns menu, checkbox mode and preference round-trip
- [x] 4.8 SchedulePendingView (replace `header.urgencyLevel` with a centered `headerExtras` entry), ScheduleCompletedView and SchedulePackingView and verify Columns menu, checkbox mode and preference round-trip
- [x] 4.9 ExceptionalReportView and verify its summary/footer behavior is unaffected by reordering

## 5. i18n

- [x] 5.1 Add a `resetColumns` key to each of the 16 view namespaces in `en`, `zhHans` and `zhHant` (48 entries) and verify each key resolves in all three locales. `CrmStaffMembersView` reuses `admin.user.*` for the rest of its copy, so it gets a dedicated `crm.staffMember.actions.resetColumns` key; `ExceptionalReportView` uses `jobOrder.orderList.actions.resetColumns` alongside the other `jobOrder.orderList.*` strings it already borrows
- [x] 5.2 Verify `JobListView` keeps its existing `jobOrder.jobList.actions.resetColumns` key and the two features are visually identical in wording

## 6. Master-Detail View (OrderListView)

- [x] 6.1 Convert the master grid: add the `#headers` slot wired to `ReorderableTableHeaders`, call `useColumnOrder` over `masterHeaders`, add `columnOrder` to its `useViewSettings` defaults, and feed the ordered headers to the table
- [x] 6.2 Pass a `reorderableKeys` allow-list so the `expander` and `#` utility columns stay pinned, and keep the expanded-row `colspan` in step with the ordered header count
- [x] 6.3 Add a "Reset column order" entry behind a divider at the end of the existing Columns menu, wired to the composable's reset, reusing `jobOrder.orderList.actions.resetColumns`
- [x] 6.4 Leave the nested line-item grid untouched: no `#headers` override, its three icon header slots and its Columns-menu visibility toggles unchanged
- [x] 6.5 Extend the shared spec with OrderListView master-grid coverage: master drag + reload persistence, pinned `expander`/`#` columns, reset, detail-column visibility still working, and the nested grid showing no drag affordance

## 7. Multi-Table View (CrmCustomer360View)

Each of the six tables (job orders, invoices, opportunities, tasks, files, emails) is converted independently; see design.md - Decision 6b. For each: add the `#headers` slot wired to `ReorderableTableHeaders`, add `columnOrder` to that table's `useViewSettings` defaults, call `useColumnOrder` over the table's `all*Headers` and default key list, bind `:headers` to the table's own `*Headers` (the resolved order filtered by `*VisibleColumnKeys`), build `*ColumnOptions` from the ordered headers, add a reset item behind a divider at the end of the Columns menu that is already there, and register that table's object id from task 3.4.

- [x] 7.1 Job orders: wire the table, keep the `ln` row-number column pinned through a `reorderableKeys` allow-list, and replace the `orderType`/`status`/`attachProduct`/`attachCustomer` icon header slots with a `joHeaderExtras` record
- [x] 7.2 Invoices, opportunities and tasks: wire the three tables on the same pattern, leaving their sort, lookup, checkbox and card/table settings untouched
- [x] 7.3 Files: wire the table; its trailing `actions` column stays reorderable, unlike a leading utility column
- [x] 7.4 Emails: wire the table and replace the `hasAttachment` icon header slot (and its `v-tooltip`) with an `emailHeaderExtras` record
- [x] 7.5 Add `customer360.jobOrders|invoices|opportunities|tasks|files|emails.actions.resetColumns` in `en`, `zhHans` and `zhHant` (18 entries)
- [x] 7.6 Verify the six tables keep their own orders, that switching tabs does not disturb them, that each menu lists and resets only its own table, that a hidden column stays hidden and keeps its place in the stored order, and that the card layouts are unaffected

## 8. Tests And Verification

- [x] 8.1 Add a Playwright spec for `ReorderableTableHeaders` with a baseline recorded from the verified JobListView rendering: header cell class list, inline width/min-width, cell count, computed padding / `position` / `text-align`, icon headers keeping icon plus accessible name, the selection header not being draggable, and drag result
- [x] 8.2 Add a cross-view behavioral spec looping the 16 converted views plus JobListView, asserting the same drag result and the same reset result rather than markup
- [x] 8.3 Add a persistence spec covering a second view: `columnOrder` written to the preference payload, restored after reload, menu order, and reset restoring the default
- [x] 8.4 Add `tests/column-order.customer360.spec.ts` (13 tests): a drag/persist/reload/menu-order/reset loop over the six tables, the pinned `ln` column, the five icon header cells, and a hidden column keeping its place in the stored order
- [x] 8.5 Run the typecheck in ClientApp and verify the error count is unchanged from the recorded baseline (59 errors, all pre-existing; the 3 pre-existing `no-unused-vars` hits in `CrmStaffMembersView`/`ExceptionalReportView`/`JobListView` and the `JobListView` sort-key comparison are among them)
- [x] 8.6 Run `npm run lint` in ClientApp and verify no new errors in the touched files
- [x] 8.7 Run the full Playwright smoke suite and verify no pre-existing spec regressed

## Verification Record

- `column-order.shared-headers.spec.ts` (new, 27 tests) is the shared coverage: a 16-view render probe, a per-view drag/persist/reload/menu-order/reset loop, the Vuetify header rendering baseline, the four icon header cells, the non-draggable selection cell, and a local-only view with no registered object id. `job-list.column-order.spec.ts` (3 tests) still passes on the shared component.
- `column-order.customer360.spec.ts` (new, 13 tests) covers the six `CrmCustomer360View` tables: a drag/persist/reload/menu-order/reset loop per table, the pinned `ln` column against both dragging and being dropped onto, the five icon header cells keeping their icon, title and `sr-only` name while staying draggable, and a hidden column staying hidden while keeping its index in the stored order.
- The Customer 360 conversion found the same real gap in the shared component that OrderListView did, and the fix in task 6.2 covers it: binding `:headers` to the unfiltered ordered list renders hidden columns again, because `visibleColumns` is a separate setting. Each table's `*Headers` is now the ordered list filtered by its own `*VisibleColumnKeys`, and each `*ColumnOptions` is built from the ordered list so the menu and the grid cannot disagree.
- The Customer 360 spec needed three fixture facts that are not documented in the view: every table renders a placeholder instead of its grid while it holds no rows, the opportunities and tasks tabs read their own lists off the company record (`company.opportunities` / `company.tasks`) and filter the `/api/v2` lists by `companyId` and `relations[].id`, and the tab window slides horizontally, so a header can be visible before it is laid out in the pane. The spec therefore waits for the first header to line up with the table's own scroll viewport before reading or dragging anything.
- The spec also reads the `reorderable-th--drop-target` cell mid-drag instead of assuming which cell the pointer lands on, because the Customer 360 grids are wider than the pane and a drop near a cell edge resolves to the target cell rather than the pixel position.
- Registering the 13 new object ids makes those views issue a preference GET on mount, which the older specs did not mock; `**/api/v2/user-preferences/**` is now mocked in the nine specs that visit an in-scope view, otherwise the unmocked request 401s and the axios interceptor bounces to `/app/login`. The six `CrmCustomer360View` ids add six more preference GETs on that view, mocked in the new spec.
- `npx vue-tsc --noEmit -p tsconfig.app.json`: 59 errors, identical to the pre-change baseline. The three in `CrmCustomer360View.vue` (`saved` unused, one `Ref<string, string>` argument, one overload) are the same pre-existing errors, verified by re-running the check with the file reverted.
- `npx eslint` on the new component, composable, specs and locale files: 0 errors. The two errors reported in `CrmCustomer360View.vue` are the pre-existing `saved` unused variable and `no-explicit-any`, also verified against the reverted file.
- Full chromium Playwright suite: 71 passed, 56 failed. The 56 failures are the same pre-existing set recorded on `HEAD`; they fail on unrelated views and selectors and are not touched by this change. The 13 extra passes are the new `CrmCustomer360View` tests.
- `OrderListView` coverage added to the shared spec: the master grid's default order, `expander` and `ln` pinned against both dragging and being dropped onto, a master drag persisted to `view-settings-orderlist` and to the preference payload, restored after reload and reverted by the Columns-menu reset, the nested line-item grid rendering Vuetify's own header row with no `.reorderable-th` and no `th[draggable]`, and the Columns menu still hiding a nested column without disturbing the master order.
- The OrderListView test found a real gap in the shared component: `reorderableKeys` only made a cell undraggable, but a drop *onto* such a cell still reordered it, so the pinned `expander` and `ln` columns could be displaced (and the same applied to `data-table-select` in every other view). `onColumnDragOver`/`onColumnDrop` now refuse a non-reorderable target, and the grab cursor moved from `.reorderable-th` to a `reorderable-th--draggable` class so a non-draggable cell no longer shows it.
- The 16-view render probe waits for a header cell instead of a fixed 600 ms, which removes a cold-start flake on the first view.
- The `mobile-chromium` project cannot launch on this host (`libflite1`, `libavif16`, `libwoff1` missing), so the phone-viewport coverage was not re-run. `OrderListView`'s card layout does not use the header arrays at all, and its table branch is changed only in the header row and the expanded-row `colspan`.

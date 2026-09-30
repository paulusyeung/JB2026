## 1. Scope Guard

- [x] 1.1 Keep the change to the 16 in-scope views and the four views with icon header cells; do not convert master-detail `OrderListView` (deferred), unused `SmlRtfListView`/`SmlInvoiceListView`/`QuotationsView`, menu-less `AdminView`/`JobOrderView`/`PublicView`/`SmlView`/`CrmCustomer360View`, or non-persistent `AdminWorkflowView`/`AdminWorkflowFormsView`/`AdminQuotationItemView`/`AdminQuotationItemGroupView`

## 2. Shared Infrastructure

- [x] 2.1 Create `useColumnOrder(columnOrder, allHeaders, defaultKeys)` returning `orderedHeaders`, `orderedColumnKeys`, `moveColumn(sourceKey, targetKey)` and `resetColumnOrder`; verify unknown keys are ignored, missing keys appended in default order, and a move is a no-op when source equals target
- [x] 2.2 Create `src/components/grids/ReorderableTableHeaders.vue` with props `table` (the `#headers` slot-props object), `headerExtras` (`Record<key, { icon, size?, color?, title?, align? }>`), `reorderableKeys?` and `reorderable?` (default `true`), emitting `move`; verify it renders the row, owns the drag state, and carries the drag-affordance styles scoped to itself
- [x] 2.3 In the component, reproduce the prototype's cell rendering exactly: `v-data-table__td v-data-table__th v-data-table__th--sticky`, `v-data-table-column--align-*`, `v-data-table-column--no-padding` on `data-table-select`, the `v-data-table-header__content` wrapper, `v-data-table-header__sort-icon`, the inline sticky style, `toCssUnit` widths, click/Enter sort, and the select-all checkbox; verify against the recorded baseline in 6.1
- [x] 2.4 Refactor JobListView onto the composable and the component: delete the inline header markup, drag handlers and drag CSS, and pass `headerExtras` for its icon columns; verify `tests/job-list.column-order.spec.ts` passes, with its header-cell locator updated from the view-owned `.job-list-th` class to the component-owned `.reorderable-th` (see design.md - Decision 1)
- [x] 2.5 Run the component spec (6.1) and the job-list spec before converting any other view, and treat a failure here as a stop rather than a per-view workaround

## 3. Preference Key Registration

- [x] 3.1 Add GUID entries to `viewPreferenceKeys.ts` for the 13 in-scope view ids that lack one: crm-opportunities, staff-members, crm-tasks, admin-user, admin-customer, admin-supplier, billing-clients, billing-invoices, billing-statement, pending-schedule, completed-schedule, packing-schedule, exceptional-report (stock, crm-people and crm-companies are already registered)
- [x] 3.2 Verify a registered view writes a preference row on first settings change and restores it on the next load
- [x] 3.3 Verify a view without a registered id still works localStorage-only (no failed requests, no console errors)

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

## 7. Tests And Verification

- [x] 7.1 Add a Playwright spec for `ReorderableTableHeaders` with a baseline recorded from the verified JobListView rendering: header cell class list, inline width/min-width, cell count, computed padding / `position` / `text-align`, icon headers keeping icon plus accessible name, the selection header not being draggable, and drag result
- [x] 7.2 Add a cross-view behavioral spec looping the 16 converted views plus JobListView, asserting the same drag result and the same reset result rather than markup
- [x] 7.3 Add a persistence spec covering a second view: `columnOrder` written to the preference payload, restored after reload, menu order, and reset restoring the default
- [x] 7.4 Run the typecheck in ClientApp and verify the error count is unchanged from the recorded baseline (59 errors, all pre-existing; the 3 pre-existing `no-unused-vars` hits in `CrmStaffMembersView`/`ExceptionalReportView`/`JobListView` and the `JobListView` sort-key comparison are among them)
- [x] 7.5 Run `npm run lint` in ClientApp and verify no new errors in the touched files
- [x] 7.6 Run the full Playwright smoke suite and verify no pre-existing spec regressed

## Verification Record

- `column-order.shared-headers.spec.ts` (new, 27 tests) is the shared coverage: a 16-view render probe, a per-view drag/persist/reload/menu-order/reset loop, the Vuetify header rendering baseline, the four icon header cells, the non-draggable selection cell, and a local-only view with no registered object id. `job-list.column-order.spec.ts` (3 tests) still passes on the shared component.
- Registering the 13 new object ids makes those views issue a preference GET on mount, which the older specs did not mock; `**/api/v2/user-preferences/**` is now mocked in the nine specs that visit an in-scope view, otherwise the unmocked request 401s and the axios interceptor bounces to `/app/login`.
- `npx vue-tsc --noEmit -p tsconfig.app.json`: 59 errors, identical to the pre-change baseline.
- `npx eslint` on the new component, composable and specs: 0 errors.
- Full chromium Playwright suite: 58 passed, 56 failed. The 56 failures are the same pre-existing set recorded on `HEAD`; they fail on unrelated views and selectors and are not touched by this change. The three extra passes are the new `OrderListView` tests.
- `OrderListView` coverage added to the shared spec: the master grid's default order, `expander` and `ln` pinned against both dragging and being dropped onto, a master drag persisted to `view-settings-orderlist` and to the preference payload, restored after reload and reverted by the Columns-menu reset, the nested line-item grid rendering Vuetify's own header row with no `.reorderable-th` and no `th[draggable]`, and the Columns menu still hiding a nested column without disturbing the master order.
- The OrderListView test found a real gap in the shared component: `reorderableKeys` only made a cell undraggable, but a drop *onto* such a cell still reordered it, so the pinned `expander` and `ln` columns could be displaced (and the same applied to `data-table-select` in every other view). `onColumnDragOver`/`onColumnDrop` now refuse a non-reorderable target, and the grab cursor moved from `.reorderable-th` to a `reorderable-th--draggable` class so a non-draggable cell no longer shows it.
- The 16-view render probe waits for a header cell instead of a fixed 600 ms, which removes a cold-start flake on the first view.
- The `mobile-chromium` project cannot launch on this host (`libflite1`, `libavif16`, `libwoff1` missing), so the phone-viewport coverage was not re-run. `OrderListView`'s card layout does not use the header arrays at all, and its table branch is changed only in the header row and the expanded-row `colspan`.

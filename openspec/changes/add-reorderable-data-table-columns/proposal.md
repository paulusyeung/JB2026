## Why

Every list view in the ClientApp renders its data table from a hard-coded header array, so column order is identical for all users and can only be changed by editing code. A working prototype now exists in `JobListView.vue`: header cells are drag-reorderable and the order is saved per user through the existing view-settings preference pipeline (localStorage + debounced server write). The 16 standard list views that are still in use and are simple enough to change now are missing that affordance, and none of them can persist preferences across devices because their view id has no object id registered in `viewPreferenceKeys.ts`.

## What Changes

- Turn the JobListView prototype into shared column-reordering infrastructure:
  - A shared `ReorderableTableHeaders` component that renders the data-table header row, including the drag affordance, drop feedback and the cell markup, preserving Vuetify sort, select-all, width/align, sticky header and icon-header behavior. Every view uses the same component, so the interaction and its styling are identical everywhere by construction.
  - A shared `useColumnOrder` composable that resolves header order from the persisted `columnOrder` setting and exposes drag handlers plus a reset action. Pure logic, no markup.
  - JobListView is refactored onto the shared pieces with no behavior change, and its existing spec is the gate before the other views are converted.
- Add drag-to-reorder column ordering to the 16 standard list views (single main data table + Columns menu + `useViewSettings`): StockView, CrmPeopleView, CrmCompaniesView, CrmOpportunitiesView, CrmStaffMembersView, CrmTasksView, AdminUserView, AdminCustomerView, AdminSupplierView, BillingClientsView, BillingInvoicesView, BillingStatementView, SchedulePendingView, ScheduleCompletedView, SchedulePackingView, ExceptionalReportView. Each conversion is the `#headers` slot, the composable call, and the reset item; the four views with icon header cells pass them as declarative data instead of a template slot.
- Make the Columns menu reflect the user's order and add a "Reset column order" action, using a `resetColumns` translation key in each view's own namespace so wording can differ per view.
- Add drag-to-reorder column ordering to the master grid of the one master-detail view, `OrderListView`. Its nine master columns become reorderable and persisted like any other view, with the leading `expander` and `#` utility columns pinned in place; the nested line-item grid keeps Vuetify's built-in header row and its existing Columns-menu visibility toggles, because that grid is rendered once per expanded row and the Columns menu already controls it.
- Persist the order per user: add a `columnOrder` entry to each view's `useViewSettings` defaults, and register object ids in `viewPreferenceKeys.ts` for the 13 in-scope views that lack one (only `stock`, `crm-people` and `crm-companies` are registered today) so preferences survive across browsers and devices. No backend change is required — `UserPreferencesController` is generic over (userId, objectType, objectId).
- Add Playwright coverage: one spec for the shared header component's contract (sorting, select-all, header layout, icon headers), plus per-view smoke coverage for drag reorder, persistence, menu order and reset.

**Out of scope** (recorded as follow-ups, no behavior change in this change):

- Views no longer in use: `SmlRtfListView`, `SmlInvoiceListView`, `QuotationsView`.
- Views with a data table but no Columns menu today: `AdminView`, `JobOrderView`, `PublicView`, `SmlView`, and the six tables in `CrmCustomer360View`. Adding a Columns menu to these is a separate UI decision.
- Views whose Columns menu is currently non-persistent (plain `ref`, lost on reload): `AdminWorkflowView`, `AdminWorkflowFormsView`, `AdminQuotationItemView`, `AdminQuotationItemGroupView`. Wiring them into `useViewSettings` is its own change.
- Column width resizing, column freezing, saved presets, and per-column alignment.

## Capabilities

### New Capabilities

- `reorderable-data-table-columns`: Users can drag data-table header cells to change column order, and that order is stored per user and restored on every load, with a reset action in the Columns menu.

### Modified Capabilities

- None.

## Impact

- Affected frontend modules:
  - `JB2026.WebApp/ClientApp/src/composables/useColumnPersistence.ts` (already carries `columnOrder`; gains no further behavior).
  - `JB2026.WebApp/ClientApp/src/composables/useColumnOrder.ts` (new: order resolution and move action, no markup).
  - `JB2026.WebApp/ClientApp/src/components/grids/ReorderableTableHeaders.vue` (new: the header row, drag affordance styling, sort and select-all rendering).
  - `JB2026.WebApp/ClientApp/src/composables/viewPreferenceKeys.ts` (object ids for the 13 in-scope view ids that lack one).
  - The 16 list views listed above (`#headers` slot wired to the component, composable call, reset item in the Columns menu; the inline header markup they do not have today is replaced rather than added).
  - `OrderListView` (master grid only: `#headers` slot, composable call with a `reorderableKeys` allow-list, reset item in the existing Columns menu; the nested line-item grid is untouched).
  - `ClientApp/src/i18n/locales/{en,zhHans,zhHant}` (one `resetColumns` key per view namespace per locale; `OrderListView` reuses `jobOrder.orderList.actions.resetColumns`).
- Affected backend modules: none. `UserPreferencesController` and the `UserPreference` table already store arbitrary (objectType, objectId) metadata per user; new object ids only add rows.
- Data: one `UserPreference` row per user per view id (objectType 1), created on first save. The 13 object ids are new server-side keys.
- Tests: one spec for the shared header component (the regression baseline for header rendering), a cross-view spec looping every converted view, master-grid coverage for `OrderListView` (pinned utility columns, nested grid left alone), and the existing job-list column-order spec, which must pass unchanged after the JobListView refactor.

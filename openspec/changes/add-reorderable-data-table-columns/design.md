## Context

See proposal.md - Why.

Technical constraints that shape the approach:

- The grid is Vuetify 3.10 `v-data-table`. Header cells are rendered internally by `VDataTableHeaders` and there is **no public API to make them draggable** and no per-column header `drag` hook. The only public extension point that covers the whole header row is the `#headers` slot, which receives `{ headers, columns, toggleSort, isSorted, getSortIcon, allSelected, someSelected, selectAll, ... }` and must render its own `<tr>`.
- `useSort` / `useSelection` are Vuetify internals and are **not** exported from the `vuetify` package entry, so a component cannot obtain sort/selection state by `inject`; the view must pass the `#headers` slot props through. Order resolution likewise needs the view's `allHeaders` array, so it stays in a composable the view calls.
- Reordering must be expressed as a reorder of the array passed to `:headers`; column identity is the header `key`, never the array index.
- `useColumnPersistence` already models a whole view-preference document in `localStorage` under `view-settings-<viewId>` and mirrors it to `UserPreferencesController` (`GET`/`PUT /api/v2/user-preferences/{objectType}/{objectId}`) with a 500 ms debounce. The prototype added a `columnOrder` array to that document. Preference metadata is free-form JSON in an XML column, so adding the field needs no migration, and older stored documents without `columnOrder` fall back to the view's default order.
- Server persistence only happens when `getViewObjectId(viewId)` returns a GUID (`viewPreferenceKeys.ts`), otherwise the view is localStorage-only. Of the 16 views in scope, only `stock`, `crm-people` and `crm-companies` are registered today, so the other 13 need a new GUID; `OrderListView` and the six `CrmCustomer360View` tables add six more.
- Audited shape of the 16 in-scope views: every one has exactly one `v-data-table` (no nested or master-detail grid), every one renders with `fixed-header`, every one has a Columns menu wired to `useViewSettings`, and every one has its own `*-table-shell` class carrying the view-scoped `:deep(.v-data-table__th)` layout rules. Four carry an icon header cell: `StockView` (`mdi-paperclip`), `CrmPeopleView` and `CrmCompaniesView` (`mdi-link-variant` with a tooltip), `SchedulePendingView` (`mdi-bell`, centered). No view needs a header cell that is more than an icon plus a title.
- Audited shape of `CrmCustomer360View`: six tables behind one tab strip (job orders, invoices, opportunities, tasks, files, emails), each with its own `useViewSettings` id, Columns menu, `visibleColumns` list, `*-table` class and `fixed-header` grid. Unlike the 16 views, one view here holds six independent tables, each with a card-layout alternative and its own sort/filter/checkbox settings, so one shared order cannot express the six. Two icon-header groups become `headerExtras` (four job-order columns, one email column), and the job-order `ln` row-number column is a utility column like `OrderListView`'s `#`.
- The prototype was verified against the previous rendering: header cell class list, inline width/min-width, computed pixel widths, header text, and body rows are identical apart from the added drag affordance and a wrapper `div` on icon-only headers.

## Goals / Non-Goals

**Goals:**

- Identical reordering UX in every view: one component renders the header row, owns the drag affordance styling, and therefore cannot differ between views.
- Per-view layout styling stays with the view (its own table class and header padding rules), while the interaction styling is defined once.
- One place where order resolution and move behavior live, so behavior cannot drift.
- A small, stable API: a converted view should cost a `#headers` slot, one composable call, and a Columns-menu item.
- No behavior change beyond reordering, verified by regression specs.

**Non-Goals:**

- Column width resizing, freezing, grouping, saved presets, per-column alignment.
- Reordering inside the mobile "sort by" chip control that Vuetify renders on small screens; that path is untouched and keeps its own ordering.
- Nested or master-detail grids; those views are out of scope for this change (proposal.md - Out of scope).
- Any backend/EF model change.

## Decisions

### 1. A shared `ReorderableTableHeaders` component renders the header row

The component lives in `src/components/grids/` beside `JobsTable.vue` and is used by all 17 tables. It receives the data table's `#headers` slot props plus declarative header extras, owns the drag state and its styling, and emits the move intent:

```vue
<template #headers="table">
  <ReorderableTableHeaders :table="table" :header-extras="headerExtras" @move="moveColumn" />
</template>
```

- `table` — the `#headers` slot-props object (`headers`, `columns`, `toggleSort`, `isSorted`, `getSortIcon`, `allSelected`, `someSelected`, `selectAll`) passed straight through. The component reads what it needs, and a slot prop Vuetify adds or renames rides along instead of breaking the call site.
- `headerExtras` — `Record<key, { icon: string; size?: number; color?: string; title?: string; align?: 'start' | 'center' | 'end' }>`, rendered as icon plus `sr-only` title. This is what the four views with icon header cells need (`{ synced: { icon: 'mdi-link-variant', size: 18, title: t('crm.people.messages.syncedTooltip') } }`), expressed as data instead of a template slot.
- `reorderableKeys` — optional allow-list of column keys that may be dragged; defaults to "everything except `data-table-select`".
- `reorderable` — boolean, default `true`. Unused (no converted view has a nested grid sharing the master's markup) but present so a future nested-grid case needs no API change.
- `@move(sourceKey, targetKey)` — the component tracks only which cell is being dragged and which is the drop target, for the visual feedback. It holds **no** ordering state, so the composable remains the single source of behavior.

**Styling split:** the drag affordance (cursor, dimmed source, drop-target marker) moves into the component's scoped styles, defined once. Each view keeps its own `*-table-shell` class and `:deep(.v-data-table__th)` padding/background rules, which legitimately differ per view and are not the affordance. The component also absorbs `toCssUnit` for width/min-width/max-width. The affordance class is therefore component-owned (`reorderable-th`, `reorderable-th--dragging`, `reorderable-th--drop-target`) rather than view-owned, so `tests/job-list.column-order.spec.ts` locates header cells with the component's class; the assertions themselves are unchanged.

- **Alternative: copy the `#headers` row into each view.** Rejected: 8 Vuetify-private names per copy (cell classes, `v-data-table-column--align-*`, `v-data-table-column--no-padding`, `v-data-table-header__content`, `v-data-table-header__sort-icon`, the inline sticky style, the `data-table-select` key, and the view-scoped `:deep(.v-data-table__th)` rules) in 17 places, ~770 duplicated lines, with a Vuetify upgrade to be replayed in every view and a visual (not functional) failure mode — wrong padding, a right-aligned column rendering left-aligned, a sticky header floating without its background. The copy also duplicates the affordance styling, which is the one thing that must not vary.
- **Alternative: patch the DOM after mount** (add `draggable` to rendered `th` elements and delegate from `thead`). Rejected: fragile against Vuetify's render cycle, no per-column key available from the DOM, and it breaks as soon as a view needs custom header content.
- **Alternative: a wrapper around `v-data-table` itself.** Rejected: the tables differ in props and slots (`fixed-header`, `loading`, alignment, per-item slots, `@click:row`), so a wrapper would either be permissive (`v-bind="$attrs"`) and lose type safety, or grow a prop per view. The `#headers` slot is the narrow, documented extension point that covers the row we need.
- **Alternative: one component for the 12 plain views, inline markup for the 4 with icon cells.** Rejected after auditing the four: every one is an icon plus a title, which `headerExtras` covers. Two implementations of the same UI would be the one thing guaranteed to drift.

### 2. Order resolution and move behavior live in a shared `useColumnOrder` composable

`useColumnOrder(columnOrder, allHeaders, defaultKeys)` returns `orderedHeaders` (default order sorted by the stored positions, unknown keys ignored, missing keys appended in default order), `orderedColumnKeys`, `moveColumn(sourceKey, targetKey)` and `resetColumnOrder`. It is pure logic with no markup and no DOM access, and it does not own persistence; the ref comes from `useViewSettings`, which already round-trips it. The view passes `orderedHeaders` to `:headers` and hands `moveColumn` to the component.

- **Alternative: the component owns ordering too.** Rejected: it would need `allHeaders` and the view's default key list, duplicating what the composable already knows, and the ordering rules would then live in the UI layer where they are harder to test and reuse.
- **Alternative: fold ordering into `useViewSettings`.** Rejected: `useViewSettings` takes a flat settings document and is also used by pivot/grid views with no `allHeaders` array; ordering needs the view's header list as input.
- **Alternative: duplicate the logic per view too.** Rejected: the logic is where drift actually shows up (which cells are draggable, what an invalid drop does, where a new column lands).

### 3. Persist a full order (including hidden columns), not just the visible sequence

The stored document keeps every column key, so hiding and re-showing a column restores the position the user gave it.

- **Alternative: store only the visible order.** Rejected: a hidden column's position is lost, and re-showing it would append it at the end.

### 4. A `reorderable` prop is available but still unused

No converted view has a nested grid that shares the master's header markup, so the prop stays unused. `OrderListView` needs no escape hatch: its master grid uses the component as-is and its nested line-item grid simply does not override `#headers`, so Vuetify keeps rendering that grid's header row.

- **Alternative: rely on a view simply not overriding `#headers` on the nested table.** Preferred, and sufficient; the prop is the escape hatch for a case where a nested table shares markup with the master.

### 5. Each view gets its own `resetColumns` i18n key (confirmed)

Every view's own namespace (`crm.people.actions.resetColumns`, `billing.invoices.actions.resetColumns`, …) gets a `resetColumns` entry in `en`, `zhHans` and `zhHant`, so wording can be adapted per view instead of forcing one string everywhere.

- **Alternative: one shared `common.resetColumns` key.** Rejected: it prevents per-view wording, which the team requires, at the cost of 16 keys x 3 locales to keep in sync by hand. JobListView's existing `jobOrder.jobList.actions.resetColumns` stays as is.

### 6. Register object ids for all in-scope views so the order is per user, not per browser (confirmed)

`viewPreferenceKeys.ts` gains a GUID for each of the 19 in-scope view ids that lacks one, so every one of the 16 views and all six `CrmCustomer360View` tables write their preference server-side. Rows appear lazily on the first preference save; no seed data, no migration step, and the generic `UserPreference` store absorbs them as ordinary (objectType 1) rows. Removing a GUID later reverts a view to localStorage-only without affecting the others.

### 6b. A view with several tables gets one order, one setting and one reset per table

`CrmCustomer360View` holds six tables, so each one owns its own `useViewSettings` id, its own `columnOrder` default, its own `useColumnOrder` call, its own `useColumnOrder` move/reset handlers and its own reset item in the Columns menu that is already there. The visibility filter stays where it was, on top of the resolved order:

```
allJoHeaders            (the view's full default header list)
  → useColumnOrder       (ordered + hidden columns keep their place)
    → orderedJoHeaders  (bound to nothing directly)
      → joHeaders       (filtered by joVisibleColumnKeys — what the grid renders)
      → joColumnOptions (the Columns menu, in the same order)
```

- The Columns menu is built from the ordered headers, not from the default list, so the menu and the grid cannot disagree; the 16 single-table views get this for free because their `*ColumnOptions` list is the whole list.
- `:headers` is bound to the filtered `joHeaders`, never to `orderedJoHeaders`. Binding the unfiltered list would render hidden columns again, because `visibleColumns` is a separate setting.
- The reset label lives in `customer360.<section>.actions.resetColumns` for each of the six sections, so the wording can be adapted per table like every other view (Decision 5).
- The card layouts keep their own hard-coded field order. They are a different presentation, not a responsive fallback of the grid, so reordering one is not required for the other to make sense.
- **Alternative: one order shared by the whole view.** Rejected: the six tables have nothing in common but a customer, and users compare unrelated column sets; one order would drag an invoice's columns into the job-order grid. One `useViewSettings` per table is what the view already does for sort, page size, checkbox and card/table mode, so this follows the established shape rather than inventing a new one.
- **Alternative: a second `v-data-table` per tab with its own route or view.** Rejected: no behavior problem is being solved, and it would split a working view into six.

### 7. Reset restores the view's default order, not a snapshot

`resetColumnOrder` writes the view's declared default key list. A user who wants the current release's layout gets it even if columns were added since their order was saved.

### 8. One heavy spec for the component, light specs per view

The expensive header assertions — cell class list, inline width/min-width, cell count, computed padding / `position` / `text-align`, icon header's icon plus accessible name, the selection header not being draggable — live in a single spec for the component, with a baseline recorded from the verified JobListView rendering. Per-view specs then only assert wiring: header order after a drag, the `columnOrder` payload, restore after reload, menu order, and reset.

- The per-view surface the component actually has to serve is small and now audited: `fixed-header` everywhere, widths and alignment per view, and icon-plus-title header cells. If a future view needs a header cell the component cannot express, the fix is a slot escape hatch on the component, reviewed once, rather than a divergence per view.
- **Alternative: a shared assertion helper run against all 17 views.** Needed under per-view markup copies (Decision 1, rejected) to catch divergence; with one component it would re-test the same code 17 times. Kept as one cross-view loop that asserts the *behavioral* invariants (same drag result, same reset result) rather than the markup.
- **Alternative: only the existing job-list spec.** Rejected: it covers one view's wiring, so a component regression that only manifests with different widths or alignment would not be caught before the other 16 views depend on it.

### 9. New and removed columns follow the prototype's existing rules (confirmed)

A column the view offers but the stored order does not contain is appended after the stored columns, in the view's default order; a stored key the view no longer offers is ignored. This is the behavior the JobListView prototype already has, and it is kept as-is rather than replaced with a fixed insertion position, so users see no change from the feature they already have and a stale order degrades instead of failing.

- **Alternative: insert a new column at a fixed position** (e.g. always after the line-number column). Rejected: it would make a user's saved order differ from what the released layout shows, and it only pays off for columns added in a specific spot. The Columns-menu reset already covers a user who wants the new release's layout.

## Risks / Trade-offs

- [A bug in the shared component takes sorting and select-all out of all 18 converted grids at once] → Land the component and refactor JobListView alone first, and require `tests/job-list.column-order.spec.ts` to pass unchanged before any other view is converted (Migration step 2). Convert the remaining views one at a time so a component defect is caught while only one view depends on it.
- [A Vuetify upgrade changes internal header markup] → One fix location. The single component spec fails first because it asserts the same class names, inline styles and computed styles against a recorded baseline, and JobListView is the reference view for repairing them.
- [Prop creep as deferred views come back] → The prop list is deliberately small and the variabilities that exist today (widths, alignment, icon cells) are data, not code. Adding a prop or an entry to `headerExtras` is preferred over a second rendering path; a genuinely new kind of header cell is the signal to reconsider the design rather than to add an escape hatch casually.
- [A view loses an icon header cell during conversion] → `headerExtras` is data, and the four conversions are explicit checklist items; the component spec covers the icon plus accessible-name contract. `StockView`'s current attachment icon has no title, so its `headerExtras` entry should add one rather than reproduce the omission.
- [Registering 13 object ids creates preference rows that did not exist server-side] → Rows are per user and per view, written only after a settings change; reversible by removing the GUID (users fall back to their local order).
- [Stored orders grow stale as columns are added or renamed] → Unknown keys are ignored and new keys are appended, so a stale order degrades gracefully instead of breaking the view.
- [`OrderListView`'s Columns menu lists the nested grid's columns, not the master grid's] → The reset entry sits at the end of that menu behind a divider, exactly as in the other 17 views, and resets the master order. The menu's own checkbox list is unchanged, so nothing that works today behaves differently; making the master grid's columns hideable is a separate UI decision.
- [`OrderListView` renders a nested grid once per expanded row, so a shared `#headers` override would multiply drag surfaces] → Only the master grid overrides `#headers`. The nested grid keeps Vuetify's built-in header row and its three icon header slots, so expanding rows costs nothing extra and the nested grid's behaviour is bit-for-bit unchanged.
- [`OrderListView`'s leading `expander` and `#` columns are utility columns] → Passed as a `reorderableKeys` allow-list so they can never be dragged, matching how `data-table-select` is treated elsewhere, and so a drop target at the far left cannot displace them.
- [`CrmCustomer360View`'s six tables each add an order, so the view is the one with the most settings in it] → The six follow the pattern the view already uses for every other per-table setting (sort, page size, checkbox, card/table mode), and the six ids are named after the table rather than the view, so a future seventh table is added the same way. The cost is six preference rows per user instead of one, which the generic store handles without a migration.
- [`CrmCustomer360View`'s job-order icon headers move from template slots to `headerExtras` and the email attachment header loses its `v-tooltip`] → `headerExtras` renders icon plus `sr-only` title and puts the title on the icon, which is what the other four icon-header views already do; the columns stay sortable-free, visually identical apart from native tooltip timing. The cell markup that replaced them was view-owned, not shared, so nothing else reads it.
- [`CrmCustomer360View`'s card layouts still use their own field order] → Accepted: the two presentations are chosen independently today, so a drag on the grid must not silently reorder a card.
- [Drag reordering is mouse-oriented on the header row] → Header cells remain click-sortable and keyboard-activatable; the Columns menu lists the same order, and the reset action is the keyboard-reachable escape hatch. Touch-based reordering is not provided.
- [A drop near a cell edge reorders to the target rather than inserting at the pixel position] → Accepted: the target index is deterministic, which keeps the stored order unambiguous; the drop-target highlight tells the user which position will result.

## Migration Plan

1. Create `useColumnOrder` and `ReorderableTableHeaders` from the prototype, and refactor JobListView onto them with no behavior change.
2. Gate on the existing job-list column-order spec plus a new component spec before touching any other view. If either fails, the change stops here with one view affected.
3. Convert the 16 views: add the `#headers` slot wired to the component, replace the four icon header slots with `headerExtras` entries, call the composable, add `columnOrder` to `useViewSettings` defaults, add the reset item and its own `resetColumns` key to the Columns menu, and register the view's object id in the same step so no view lands half-persistent.
4. Convert `OrderListView` last, on the same pattern: master grid only, `reorderableKeys` allow-list for the reorderable columns, reset entry in the existing Columns menu, `columnOrder` in its `useViewSettings` defaults, and the nested line-item grid left alone. Its `orderlist` object id is already registered.
5. Convert the six `CrmCustomer360View` tables on the same pattern, one table per step: `#headers` slot, `columnOrder` in the table's `useViewSettings` defaults, a `useColumnOrder` call, the Columns menu rebuilt from the ordered headers plus a reset item, `headerExtras` for the icon headers, the `reorderableKeys` allow-list for job orders, and a `customer360.<section>.actions.resetColumns` key in all three locales. Each table's object id is registered in the same step.
6. No data migration, no backend deploy, no feature flag. Rollback is a revert of the frontend change; a stored `columnOrder` in existing preferences is inert for views that stop reading it, and the Columns-menu reset gives users a way back without a deploy.

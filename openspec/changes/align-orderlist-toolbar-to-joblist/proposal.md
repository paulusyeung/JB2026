## Why

The first four toolbar actions in OrderList and JobList currently diverge in ordering and visual pattern, which creates avoidable context switching for users moving between both pages. Aligning OrderList to JobList improves consistency, reduces misclicks, and supports a predictable workflow.

## What Changes

- The **first four** OrderList toolbar controls SHALL be, in order: **columns**, **sorting**, **checkbox**, and **views** — matching `JobListView.vue` in interaction model, structure, and Vuetify styling (outlined actions, menus, icons).
- **Views** (fourth slot) SHALL mirror JobList: activator with `mdi-eye-outline`, menu with detail vs card entries, active row highlighting, and the same `viewMode` / `setViewMode` behavior adapted to OrderList’s layouts (table vs card presentation).
- Update OrderList **desktop** toolbar so those four match `JobListView.vue`; update **phone** overflow so checkbox + view-mode entries (and their order/active states) match `JobListView.vue`, with columns and sort remaining as always-visible activators on the bar (same as JobList).
- After the first four, use the **same divider-then-rest pattern** as `JobListView.vue` for the remaining actions; keep existing OrderList business semantics for print, export, new, delete, and batch delete. Add or reorder toolbar items only where needed to mirror JobList (e.g. attachment before print if parity requires it).
- Keep compatibility with current i18n keys where possible; reuse `jobOrder.jobList.actions` (and related) for shared concepts such as **views** where strings should match JobList; add OrderList-specific keys only when needed.

## Capabilities

### New Capabilities
- `orderlist-toolbar-action-parity`: Standardize the first four OrderList toolbar actions to **columns, sorting, checkbox, views**, matching `JobListView.vue` on desktop and mobile.

### Modified Capabilities
- None.

## Impact

- Affected UI files:
  - `JB2026.WebApp/ClientApp/src/views/OrderListView.vue` (primary implementation; introduce views fourth slot and view-mode behavior aligned with JobList).
  - `JB2026.WebApp/ClientApp/src/views/JobListView.vue` (**baseline reference** for markup, styles, and behavior; no change expected unless a shared fix is explicitly needed).
- Potential i18n touchpoints:
  - `JB2026.WebApp/ClientApp/src/i18n/locales/en/jobOrder.ts`
  - `JB2026.WebApp/ClientApp/src/i18n/locales/zhHans/jobOrder.ts`
  - `JB2026.WebApp/ClientApp/src/i18n/locales/zhHant/jobOrder.ts`
- No backend/API changes expected.
- No database or dependency changes expected.

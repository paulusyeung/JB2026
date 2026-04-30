## Why

The JobList toolbar currently has a "Popup" button that opens the editor for the active row, but lacks a view mode toggle. StockView already has a "Views" button that switches between table and card layouts. Aligning JobList to use the same pattern provides a consistent user experience and gives users the flexibility to view job orders in either table or card format.

Exploration also identified two important experience gaps that must be handled in-scope:

1. JobList currently does not use `useViewSettings`, so there is no persisted view mode behavior yet.
2. Backend persistence for view settings depends on a registered view key/object id mapping. Without adding a JobList mapping, persistence would be local-only (browser storage) instead of user-level persistence.

## What Changes

- **Replace** the "Popup" button in `JobListView.vue` with a "Views" dropdown button (matching StockView's pattern).
- **Add** a view mode toggle (`detail` / `card`) to JobList, persisted via `useViewSettings` (same composable used in StockView).
- **Add** JobList view-key registration in `viewPreferenceKeys` so `useViewSettings('joblist', ...)` persists through the backend user preferences store (not local-only).
- **Add** a card/mobile layout for job orders in JobList (similar to `stock-mobile-card` in StockView), triggered when view mode is set to `card`.
- **Adapt** JobList card CSS for desktop multi-column rendering, so card mode is usable beyond phone width.
- **Add** corresponding i18n keys for the new "Views" button and view mode labels in the `jobOrder.jobList.actions` namespace.
- **Remove** the standalone "Popup" button from both desktop and mobile toolbar menus in JobList.
- **Align interaction behavior** so both table row click and card click open the editor after Popup removal.

## Capabilities

### New Capabilities
- `joblist-view-toggle`: Toggle between table (detail) and card views in the job list page, with persisted settings.

### Modified Capabilities
<!-- None — no existing specs to modify. -->

## Impact

- **Affected files**:
  - `ClientApp/src/views/JobListView.vue` — toolbar buttons, template logic, script setup (view mode state, composable).
  - `ClientApp/src/composables/viewPreferenceKeys.ts` — add stable object id mapping for `joblist` view settings persistence.
  - `ClientApp/src/composables/useColumnPersistence.ts` (existing behavior relied on) — no logic changes expected, but this change now depends on server save/load path being active for JobList.
  - `ClientApp/src/i18n/locales/en/jobOrder.ts`, `ClientApp/src/i18n/locales/zhHans/jobOrder.ts`, `ClientApp/src/i18n/locales/zhHant/jobOrder.ts` — new views-related action labels.
- **No new API endpoints** — existing user preference API is reused.
- **No breaking changes** — existing table view remains the default.

## Validation Focus

- Verify detail view remains default for first-time users.
- Verify card/detail preference persists across page reload and across browser sessions for the same user.
- Verify card mode uses desktop-friendly multi-column layout and mobile-friendly single-column layout.
- Verify Popup action is removed from desktop/mobile menus, while editor access remains available via table row and card click.

</contents>
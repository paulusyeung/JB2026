## 1. Add i18n Keys

- [x] 1.1 Add `jobOrder.jobList.actions.views` translation key for the Views button label
- [x] 1.2 Add `jobOrder.jobList.actions.detailView` translation key for the Detail View menu option
- [x] 1.3 Add `jobOrder.jobList.actions.cardView` translation key for the Card View menu option
- [x] 1.4 Add/update these keys in all active locales (`en`, `zhHans`, `zhHant`) for `jobOrder.jobList.actions`

## 2. Add View Mode State and Composable

- [x] 2.1 Add `joblist` entry to view preference key/object id mapping so `useViewSettings('joblist', ...)` persists through backend user preferences
- [x] 2.2 Import `useViewSettings` composable in JobListView.vue
- [x] 2.3 Initialize `useViewSettings('joblist', ...)` with default settings (visible columns, sort key/direction, checkbox mode, view mode defaulting to `'detail'`)
- [x] 2.4 Replace existing `ref` declarations for `visibleColumnKeys`, `sortKey`, `sortDirection`, and `checkboxMode` with the persisted values from `useViewSettings`
- [x] 2.5 Add `isCardView` computed property (`viewMode.value === 'card'`)

## 3. Replace Popup Button with Views Dropdown

- [x] 3.1 Remove the standalone "Popup" button from the desktop toolbar (`v-if="!isPhoneLayout"` block)
- [x] 3.2 Add a "Views" dropdown menu in the desktop toolbar, matching StockView's pattern (two list items: Detail View and Card View with active state)
- [x] 3.3 Remove the "Popup" list item from the mobile overflow menu (`v-else` block)
- [x] 3.4 Add Detail View and Card View list items to the mobile overflow menu, matching StockView's pattern
- [x] 3.5 Add `setViewMode` function to update `viewMode.value`

## 4. Adapt Card Layout for Desktop Card View

- [x] 4.1 Change the `v-if="isPhoneLayout"` / `v-else` split to `v-if="isCardView"` / `v-else` so card mode works regardless of screen size
- [x] 4.2 Add CSS for multi-column card grid on desktop (e.g., `grid-template-columns: repeat(auto-fill, minmax(320px, 1fr))` on `.job-mobile-list` when in card mode)
- [x] 4.3 Ensure the card template displays all relevant job order fields (order number, customer, status, dates, amounts) similar to the existing mobile card

## 5. Verify and Clean Up

- [x] 5.1 Verify table view is the default on first load
- [x] 5.2 Verify view mode persists across page reload
- [x] 5.3 Verify clicking a row in table view opens the editor dialog
- [x] 5.4 Verify clicking a card in card view opens the editor dialog
- [x] 5.5 Verify checkbox mode, column toggle, and sorting still work correctly with the new setup
- [x] 5.6 Remove any unused imports or dead code from the refactoring
- [x] 5.7 Verify view mode persistence across browser sessions for the same user (proves backend persistence, not local-only)
- [x] 5.8 Verify Popup action is fully removed from desktop toolbar and mobile overflow menu
- [x] 5.9 Verify desktop card mode renders multi-column cards and mobile card mode remains single-column/phone-friendly

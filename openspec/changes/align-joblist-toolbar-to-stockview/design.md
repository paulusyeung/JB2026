## Context

JobListView.vue currently uses a static toolbar with a "Popup" button that opens the editor dialog for the currently selected row. StockView.vue already implements a more flexible toolbar pattern with a "Views" dropdown that toggles between table (`detail`) and card (`card`) layouts. The view mode, column visibility, sort settings, and checkbox mode in StockView are all persisted using the `useViewSettings` composable.

The goal is to bring JobList in line with this pattern so users get a consistent experience across both pages.

## Goals / Non-Goals

**Goals:**
- Replace the "Popup" button in JobList with a "Views" dropdown matching StockView.
- Add card view rendering for job orders in JobList.
- Persist view mode preference using `useViewSettings` composable.
- Maintain backward compatibility — table view remains the default.

**Non-Goals:**
- Changing the underlying data fetching or API contracts.
- Modifying StockView.vue (it already has the target pattern).
- Adding new columns or changing existing column behavior.

## Decisions

1. **Use `useViewSettings` composable for persistence**
   - StockView already uses `useViewSettings('stock', ...)` to persist visible columns, sort key/direction, checkbox mode, and view mode. JobList will adopt the same composable with key `'joblist'`.
   - *Alternative considered*: Manual localStorage — rejected because `useViewSettings` already provides a tested, reusable abstraction.

2. **Default view mode is `detail` (table)**
   - Existing users expect the table layout. Card view is opt-in.

3. **Card layout mirrors existing mobile card structure**
   - JobList already has a `job-mobile-card` layout used on phone screens (`isPhoneLayout`). The card view will reuse this same card template but trigger based on `viewMode === 'card'` rather than screen width.
   - The existing `v-if="isPhoneLayout"` / `v-else` split will change to `v-if="isCardView"` / `v-else` so card mode works on desktop too.

4. **Remove "Popup" from both desktop and mobile menus**
   - The Popup functionality is replaced by clicking a row/card to open the editor, which already works. The explicit button is redundant once card view is available.

## Risks / Trade-offs

- **Card layout on wide screens**: The mobile card CSS was designed for narrow viewports. On desktop, cards may need a grid layout (e.g., `repeat(auto-fill, minmax(320px, 1fr))`) rather than a single-column stack. → *Mitigation*: Adapt the card grid CSS for desktop card mode, similar to how StockView handles it with `stock-mobile-list`.
- **State migration**: Existing users have no persisted view mode for JobList. → *Mitigation*: `useViewSettings` defaults to `'detail'`, so no migration is needed.

## Migration Plan

No migration needed. This is a frontend-only change with no data model or API modifications. The default behavior (table view) is unchanged.

## Open Questions

None at this time.

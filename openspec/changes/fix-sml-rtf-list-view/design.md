## Context

The SML RTF List View in the legacy application (RtfList_v5.cs) implements a master-details DataGridView pattern where each master row (RTF header) is expandable to show child rows (line items with product details). The current Vue 3 port (SmlRtfListView.vue) is a flat table without any hierarchical or expandable structure, missing critical product line information that users rely on for invoice verification and data review.

The legacy implementation uses a single master-details DataGrid control with event handling for row expansion, automatic child grid styling, and nested data binding. The port must replicate this UX pattern using Vue 3 and Vuetify components.

**Key Finding**: The existing `GET /api/v2/sml/rtf-list` endpoint already returns line items nested within each header's `Items` array. The frontend TypeScript types (`SmlRtfListItem`, `SmlRtfListHeader`) already define this structure. No backend changes or new API endpoints are needed—this is purely a frontend UI change to display data that's already being fetched.

## Goals / Non-Goals

**Goals:**
- Replicate the master-details hierarchical table structure from the legacy app
- Enable users to expand RTF header rows to view associated line items
- Display line item product details: code, description, price, quantity, amount
- Add DNCount column to master table (available in backend but not currently displayed)
- Maintain feature parity with legacy filters and search functionality
- Ensure consistent styling and spacing for expanded child rows
**Non-Goals:**
- Add new features beyond legacy parity (e.g., inline editing, bulk operations)
- Refactor the master row columns significantly; maintain existing master grid columns
- Implement real-time update detection or change notifications
- Create new backend endpoints or modify existing API contracts
- Add ModifiedOn/ModifiedBy columns (these fields don't exist in the backend model; only CreatedOn/CreatedBy exist, which are already displayed)
## Decisions

**1. Expandable Row Implementation Approach**
- **Decision**: Use Vuetify `v-data-table` with `expanded` state for the master rows, and render a nested `v-data-table` for child items
- **Rationale**: Vuetify's built-in expansion support avoids custom DOM manipulation and integrates cleanly with Vue 3 reactivity. Simple to style and control via component state.
- **Alternatives Considered**: 
  - Custom collapse/expand button with conditional child table rendering (more flexible but verbose)
  - Accordion pattern with separate expand/collapse logic (overkill for this use case)

**2. Data Source**
- **Decision**: Use the `items` array already present in each header from the existing API response
- **Rationale**: The `GET /api/v2/sml/rtf-list` endpoint already returns `SmlRtfListHeaderResponse` with an `Items` property containing `SmlRtfListItemResponse[]`. The frontend types already reflect this. No additional API calls, caching, or loading states are needed for line item data.
- **Alternatives Considered**:
  - Lazy-load via separate API call (unnecessary complexity—data is already in memory)
  - Batch-fetch all line items upfront (already happening—no change needed)

**3. State Management**
- **Decision**: Use Vue component-level `ref` state to track which rows are expanded
- **Rationale**: SmlRtfListView is a self-contained feature module. No global state needed; local ref state is sufficient. Line item data doesn't need caching since it's already loaded with the master data.
- **Alternatives Considered**:
  - Pinia store (over-engineered for local expand/collapse state)

**4. Child Row Styling**
- **Decision**: Apply CSS classes for multi-row height and text alignment; use Vuetify theme defaults for colors/spacing
- **Rationale**: Legacy behavior uses 32px row height for wrapped content. Match via `scoped` CSS with specific row class selectors.
- **Alternatives Considered**:
  - Inline styles (hard to maintain, scattered logic)
  - Tailwind classes (not available; project uses Vuetify)

## Risks / Trade-offs

| Risk | Mitigation |
|------|-----------|
| Large initial payload (500 headers × many line items each) → slow initial load | Monitor payload size. If response exceeds ~2MB, consider adding pagination to the master endpoint (separate change). For now, the data is already being fetched, so no regression. |
| Column alignment mismatch between master/child tables | Test child table column widths carefully; use consistent formatting functions for numeric values |
| Child table overflow on narrow screens | Test responsive layout; consider horizontal scrolling or column hide for mobile |
| Memory usage with many expanded rows simultaneously | Allow multiple rows expanded (matches legacy). If memory becomes an issue, consider single-expand mode. |

## Migration Plan

1. **Phase 1: Component Implementation**
   - Refactor SmlRtfListView.vue to use `expanded` state for rows
   - Add child row rendering template using existing `item.items` data
   - Add DNCount column to master table
2. **Phase 2: Testing & Validation**
   - Manual testing of expand/collapse with sample data
   - Performance testing with 500+ rows
   - Cross-browser responsive testing

3. **Phase 3: Rollout**
   - Deploy directly—no backend changes, no API contract changes
   - Monitor for any payload size issues
## Open Questions

- Are there any permission/visibility rules that affect which line items are shown per user?
- What is the expected maximum line item count per RTF header? (Affects whether we need to worry about payload size)
- Should we enforce single-expand mode (only one row at a time) or allow multiple rows expanded simultaneously? Legacy allows multiple.


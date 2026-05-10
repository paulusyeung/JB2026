## Context

The SML Invoice List View in the legacy application implements a master-details DataGridView pattern where each master row (invoice header) is expandable to show child rows (line items with product details). The current Vue 3 port (SmlInvoiceListView.vue) is a flat table without any hierarchical or expandable structure, missing critical line item information that users rely on for invoice verification and data review.

The legacy implementation uses a single master-details DataGrid control with event handling for row expansion, automatic child grid styling, and nested data binding. The port must replicate this UX pattern using Vue 3 and Vuetify components.

**Key Difference from RTF List Fix**: Unlike the RTF list endpoint which already returns line items, the invoice list endpoint `GET /api/v2/sml/invoice-list` currently does NOT return line item data. The backend `SmlInvoiceListRowResponse` model lacks an `Items` property. This change requires:
1. Backend modifications to query `InvoiceItem` and `InvoiceSubItem` tables
2. New response model with line item data
3. Frontend implementation to display the expandable structure

The existing EF Core entities `InvoiceItem` and `InvoiceSubItem` provide access to the line item data through the `JB5LegacyReadContext`.

## Goals / Non-Goals

**Goals:**
- Replicate the master-details hierarchical table structure from the legacy app
- Enable users to expand invoice header rows to view associated line items
- Display line item product details: line number, description, quantity, unit, price, amount
- Add backend support to return line items in the invoice list response
- Maintain feature parity with legacy filters and search functionality
- Ensure consistent styling and spacing for expanded child rows (matching RTF list pattern)
- Implement proper state management for expandable row toggle functionality

**Non-Goals:**
- Add new features beyond legacy parity (e.g., inline editing, bulk operations)
- Refactor the master row columns significantly; maintain existing master grid columns
- Implement real-time update detection or change notifications
- Add ModifiedOn/ModifiedBy columns to the master table (these fields exist in the backend but are not currently displayed)
- Implement card view mode for mobile (can be added in future iteration)
- Add column visibility toggles or sorting controls (can be added in future iteration)

## Decisions

**1. Backend Data Source for Line Items**
- **Decision**: Query `InvoiceItem` and `InvoiceSubItem` EF Core entities directly through `JB5LegacyReadContext`
- **Rationale**: These entities already exist and are properly configured. The `InvoiceItem` table links to invoice headers via `HeaderId`, and `InvoiceSubItem` contains the actual line item details (description, quantity, unit, price, amount).
- **Alternatives Considered**:
  - Create a new database view (overkill for this use case)
  - Use stored procedures (inconsistent with current endpoint pattern)
  - Query raw SQL (less maintainable than EF Core)

**2. Expandable Row Implementation Approach**
- **Decision**: Use Vuetify `v-data-table` with `expanded` state for the master rows, and render a nested `v-data-table` for child items
- **Rationale**: Vuetify's built-in expansion support avoids custom DOM manipulation and integrates cleanly with Vue 3 reactivity. Simple to style and control via component state. Matches the pattern already established in SmlRtfListView.vue.
- **Alternatives Considered**:
  - Custom collapse/expand button with conditional child table rendering (more flexible but verbose)
  - Accordion pattern with separate expand/collapse logic (overkill for this use case)

**3. Response Model Structure**
- **Decision**: Add `Items` property to `SmlInvoiceListRowResponse` containing `IReadOnlyList<SmlInvoiceListItemResponse>`
- **Rationale**: Consistent with the existing `SmlRtfListHeaderResponse` pattern. Maintains backward compatibility as existing fields remain unchanged.
- **Alternatives Considered**:
  - Separate endpoint for line items (unnecessary complexity—data should be loaded together)
  - Flatten the response (loses hierarchical structure)

**4. State Management**
- **Decision**: Use Vue component-level `ref` state to track which rows are expanded
- **Rationale**: SmlInvoiceListView is a self-contained feature module. No global state needed; local ref state is sufficient. Line item data doesn't need caching since it's already loaded with the master data.
- **Alternatives Considered**:
  - Pinia store (over-engineered for local expand/collapse state)

**5. Child Row Styling**
- **Decision**: Apply CSS classes for multi-row height and text alignment; use Vuetify theme defaults for colors/spacing. Match the RTF list detail panel styling.
- **Rationale**: Legacy behavior uses 32px row height for wrapped content. Match via `scoped` CSS with specific row class selectors. Consistent with the existing RTF list implementation.
- **Alternatives Considered**:
  - Inline styles (hard to maintain, scattered logic)
  - Tailwind classes (not available; project uses Vuetify)

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| Large initial payload (500 headers × many line items each) → slow initial load | Monitor payload size. If response exceeds ~2MB, consider adding pagination to the master endpoint (separate change). For now, the data is already being fetched, so no regression. |
| Column alignment mismatch between master/child tables | Test child table column widths carefully; use consistent formatting functions for numeric values |
| Child table overflow on narrow screens | Test responsive layout; consider horizontal scrolling or column hide for mobile |
| Memory usage with many expanded rows simultaneously | Allow multiple rows expanded (matches legacy). If memory becomes an issue, consider single-expand mode. |
| Backend query performance with JOIN operations | Use `AsNoTracking()` for read-only queries. Index the `HeaderId` foreign key if not already indexed. |

## Migration Plan

1. **Phase 1: Backend Implementation**
   - Add `SmlInvoiceListItemResponse` model class
   - Add `Items` property to `SmlInvoiceListRowResponse`
   - Modify `GetInvoiceList` endpoint to query line items
   - Update frontend TypeScript types

2. **Phase 2: Frontend Implementation**
   - Refactor SmlInvoiceListView.vue to use `expanded` state for rows
   - Add child row rendering template using existing `item.items` data
   - Add expand icon column and handlers

3. **Phase 3: Testing & Validation**
   - Manual testing of expand/collapse with sample data
   - Performance testing with 500+ rows
   - Cross-browser responsive testing

4. **Phase 4: Rollout**
   - Deploy backend and frontend together
   - Monitor for any payload size issues
   - Verify backward compatibility

## Open Questions

- Are there any permission/visibility rules that affect which line items are shown per user?
- What is the expected maximum line item count per invoice header? (Affects whether we need to worry about payload size)
- Should we enforce single-expand mode (only one row at a time) or allow multiple rows expanded simultaneously? Legacy allows multiple.
- Should the line items be sorted by line number by default?

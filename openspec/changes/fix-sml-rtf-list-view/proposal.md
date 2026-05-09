## Why

The current SML RTF List View port lacks the master-details hierarchical table structure present in the legacy application. Users cannot expand rows to view line item details (product codes, descriptions, pricing, quantities), making the UI incomplete and non-functional compared to the original design. This significantly reduces usability for invoice inspection and data verification workflows.

## What Changes

- Implement expandable/collapsible row structure for the RTF invoice list
- Add details grid showing line items when a master row is expanded
  - Display product information: code, description, price, quantity, amount
  - Enable users to see complete invoice composition without navigation
- Add missing master grid column: DNCount (available in backend but not displayed)
- Style detail rows consistently with legacy behavior (double-row height, proper alignment)
- Implement proper state management for expandable row toggle functionality

**Note**: Line item data is already returned by the existing `GET /api/v2/sml/rtf-list` endpoint nested within each header's `Items` array. No new API endpoint or backend changes are required. This is a frontend-only UI change.

## Capabilities

### New Capabilities
- `sml-rtf-detail-expansion`: Expandable rows in RTF invoice list showing nested line items with product details
- `sml-rtf-line-items-display`: Display of line item details (code, description, price, qty, amount) in child grid

### Modified Capabilities
- `sml-rtf-invoice-list`: RTF invoice list now displays as master-details instead of flat table with DNCount column added

## Impact

**Frontend Code**
- `JB2026.WebApp/ClientApp/src/views/SmlRtfListView.vue`: Major refactor to implement expandable structure
- `JB2026.WebApp/ClientApp/src/types/api.ts`: Types for line items (`SmlRtfListItem`, `SmlRtfListHeader.items`) already exist and are correct
**Backend Code**
- No backend changes required. The existing `GET /api/v2/sml/rtf-list` endpoint already returns line items nested in each header's `Items` array.
**APIs**
- No new API endpoints needed. Existing endpoint already provides all required data.
**Dependencies**
- Vuetify v-data-table with nested expansion support
- Potentially custom styling for multi-row expansion behavior


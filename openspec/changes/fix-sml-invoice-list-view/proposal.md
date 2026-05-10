## Why

The current SML Invoice List View port lacks the master-details hierarchical table structure present in the legacy application. Users cannot expand invoice header rows to view line item details (line numbers, descriptions, quantities, units, prices, amounts), making the UI incomplete and non-functional compared to the original design. This significantly reduces usability for invoice inspection, data verification, and audit workflows.

Unlike the RTF list fix, the invoice list endpoint currently does **not** return line item data. The backend `SmlInvoiceListRowResponse` model lacks an `Items` property, and the `GET /api/v2/sml/invoice-list` endpoint does not query the `InvoiceItem`/`InvoiceSubItem` tables. This change requires both backend modifications (to include line items in the response) and frontend implementation (to display them in an expandable structure).

## What Changes

- **Backend**: Extend `SmlInvoiceListRowResponse` with an `Items` property containing line item details
- **Backend**: Modify `GET /api/v2/sml/invoice-list` endpoint to query `InvoiceItem` and `InvoiceSubItem` tables and include line items in the response
- **Frontend**: Implement expandable/collapsible row structure for the invoice list
- **Frontend**: Add details grid showing line items when a master row is expanded
  - Display product information: line number, description, quantity, unit, price, amount
  - Enable users to see complete invoice composition without navigation
- **Frontend**: Add new TypeScript types for invoice line items (`SmlInvoiceListItem`)
- **Frontend**: Style detail rows consistently with the RTF list pattern (light background, proper alignment)
- **Frontend**: Implement proper state management for expandable row toggle functionality

## Capabilities

### New Capabilities
- `sml-invoice-detail-expansion`: Expandable rows in invoice list showing nested line items with product details
- `sml-invoice-line-items-display`: Display of line item details (line number, description, qty, unit, price, amount) in child grid
- `sml-invoice-line-items-backend`: Backend endpoint returns line item data nested within each invoice header

### Modified Capabilities
- `sml-invoice-list`: Invoice list now displays as master-details with expandable rows and line item data

## Impact

**Frontend Code**
- `JB2026.WebApp/ClientApp/src/views/SmlInvoiceListView.vue`: Major refactor to implement expandable structure
- `JB2026.WebApp/ClientApp/src/types/api.ts`: New type `SmlInvoiceListItem` and updated `SmlInvoiceListRow` with `items` property

**Backend Code**
- `JB2026.Api/Models/SmlInvoiceListResponse.cs`: New `SmlInvoiceListItemResponse` class and `Items` property on `SmlInvoiceListRowResponse`
- `JB2026.Api/Controllers/SmlController.cs`: Modified `GetInvoiceList` endpoint to query and include line items

**APIs**
- `GET /api/v2/sml/invoice-list`: Response shape changes to include nested `Items` array per header (backward compatible - existing fields unchanged)

**Dependencies**
- Vuetify v-data-table with nested expansion support (already used in RTF list)
- Existing `InvoiceItem` and `InvoiceSubItem` EF Core entities


## 1. Backend Model Changes

- [ ] 1.1 Create `SmlInvoiceListItemResponse` model class with properties: LineNumber, Description, Quantity, Unit, Price, Amount
- [ ] 1.2 Add `Items` property to `SmlInvoiceListRowResponse` of type `IReadOnlyList<SmlInvoiceListItemResponse>`

## 2. Backend Endpoint Implementation

- [ ] 2.1 Modify `GetInvoiceList` endpoint to query `InvoiceItem` and `InvoiceSubItem` tables
- [ ] 2.2 Join invoice headers with line items by `HeaderId`
- [ ] 2.3 Map line item data to `SmlInvoiceListItemResponse` objects
- [ ] 2.4 Include line items in the response for each invoice header
- [ ] 2.5 Ensure empty array is returned for invoices without line items

## 3. Frontend Type Definitions

- [ ] 3.1 Add `SmlInvoiceListItem` interface to `api.ts` with properties: lineNumber, description, quantity, unit, price, amount
- [ ] 3.2 Update `SmlInvoiceListRow` interface to include `items: SmlInvoiceListItem[]` property

## 4. Frontend Expandable Row Structure

- [ ] 4.1 Add `expandedHeaderIds` ref state to track which rows are expanded
- [ ] 4.2 Add expand icon column to the master table headers
- [ ] 4.3 Implement expand/collapse icon template for each row
- [ ] 4.4 Add `toggleExpandedRow` function to handle row expansion
- [ ] 4.5 Add `isExpanded` helper function to check expansion state

## 5. Frontend Child Table Rendering

- [ ] 5.1 Add `expanded-row` template slot to render child table
- [ ] 5.2 Define child table headers (Line Number, Description, Quantity, Unit, Price, Amount)
- [ ] 5.3 Render nested `v-data-table` for line items
- [ ] 5.4 Format numeric values (quantity, price, amount) with proper locale formatting
- [ ] 5.5 Handle empty state when no line items exist

## 6. Styling and Polish

- [ ] 6.1 Add CSS classes for child table background and spacing
- [ ] 6.2 Ensure child table row height matches RTF list pattern (32px minimum)
- [ ] 6.3 Add proper text alignment for numeric columns
- [ ] 6.4 Test responsive layout for narrow screens

## 7. State Management and Edge Cases

- [ ] 7.1 Clear expanded state when refresh button is clicked
- [ ] 7.2 Handle error states gracefully
- [ ] 7.3 Ensure loading state is displayed during data refresh

## 8. Testing and Validation

- [ ] 8.1 Test expand/collapse functionality with sample data
- [ ] 8.2 Verify line item data accuracy against backend
- [ ] 8.3 Test with invoices that have no line items
- [ ] 8.4 Test with large datasets (500+ rows)
- [ ] 8.5 Cross-browser responsive testing

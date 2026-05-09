## 1. Setup and Analysis

- [x] 1.1 Review legacy RtfList_v5.cs DataGridView expansion logic and event handlers
- [x] 1.2 Check existing SmlRtfListView.vue component and identify all current columns and filters
- [x] 1.3 Verify that `item.items` array is populated in the current API response (confirm line items are already present)

## 2. Master Table Enhancement

- [x] 2.1 Add DNCount column to master table headers (use `item.dnCount` from existing response)
- [x] 2.2 Update column definitions to include proper widths and alignments for DNCount
- [x] 2.3 Verify all existing master columns display correct data and formatting

## 3. Expandable Row Implementation

- [x] 3.1 Add component-level `ref` to track expanded row IDs (array of header IDs)
- [x] 3.2 Convert `v-data-table` to support expandable rows using Vuetify's `expanded` property
- [x] 3.3 Add expand icon column using Vuetify slot `#item.data-table-expand`
- [x] 3.4 Implement `@update:expanded` handler to track expand/collapse events
- [x] 3.5 Set table property to allow multiple rows expanded simultaneously (matches legacy behavior)

## 4. Child Table Implementation

- [x] 4.1 Create child table template in `#expanded-row` slot for displaying line items
- [x] 4.2 Bind child table items to `item.items` (already in memory, no API call needed)
- [x] 4.3 Define child table headers: Line Number, Product Code, Product Description, Price, Quantity, Amount
- [x] 4.4 Set column widths matching legacy behavior (line: 70px, code: 180px, desc: 300px+, amount: 130px right-aligned)
- [x] 4.5 Implement child table as read-only (no editing, adding, or deleting)
- [x] 4.6 Set child table row height to 32px minimum for multi-line text wrapping

## 5. Styling and Appearance

- [x] 5.1 Add scoped CSS class for child table background color (WhiteSmoke/light gray)
- [x] 5.2 Add CSS for child row height (32px)
- [x] 5.3 Style master row borders and spacing to distinguish from child rows
- [x] 5.4 Verify column alignment: left for text, right for numeric values
- [ ] 5.5 Test responsive layout on narrow screens (consider horizontal scroll if needed)

## 6. Empty State and Placeholder Handling

- [x] 6.1 Display "No line items" message in child table when header has zero line items
- [x] 6.2 Handle case where `item.items` is undefined or null defensively

## 7. Formatting and Localization

- [x] 7.1 Use `activeLocale` from `useLocaleFormatters()` for number formatting in child table
- [x] 7.2 Apply consistent formatting to Price, Quantity, and Amount columns
- [x] 7.3 Verify date/time formats in master table match user locale
- [ ] 7.4 Test with multiple locale settings (HK, US, EU, etc.)

## 8. Component Integration

- [x] 8.1 Ensure filter, lookup, and common query functionality still work with expandable structure
- [ ] 8.2 Test that expanding rows doesn't interfere with filtering or sorting
- [x] 8.3 Verify refresh button reloads master data and clears expanded rows
- [ ] 8.4 Ensure expanded rows collapse when navigating away and returning

## 9. Testing

- [ ] 9.1 Manual test: Expand a row and verify child table appears with line items
- [ ] 9.2 Manual test: Collapse and re-expand same row; verify data persists (no flicker, no reload)
- [ ] 9.3 Manual test: Expand multiple rows simultaneously; verify independent states
- [ ] 9.4 Manual test: Test with row that has no line items; verify empty state
- [ ] 9.5 Manual test: Search/filter with expanded rows; verify child tables persist or clear appropriately
- [ ] 9.6 Manual test: Refresh page; verify all rows collapse and state resets
- [ ] 9.7 Responsive test: Verify layout on mobile, tablet, and desktop screens
- [ ] 9.8 Performance test: Expand/collapse 50+ rows; check for memory leaks or slowdowns
- [ ] 9.9 Keyboard navigation test: Tab through rows and use Space/Arrow keys to expand/collapse

## 10. Final Review and Cleanup

- [x] 10.1 Review i18n keys for all new column headers and messages
- [x] 10.2 Clean up any unused imports or dead code
- [ ] 10.3 Verify no console warnings or errors in Vue devtools


# Tasks - port-stock-product-record-popup

## Group 1: Discovery and Contract Alignment

- [x] Review current stock API capabilities and confirm product detail CRUD endpoint mapping
- [x] Capture legacy ProductRecord field and validation parity checklist for implementation reference
- [x] Define service contract in ClientApp for product record operations and movement history

## Group 2: Dialog Component Implementation

- [x] Create ProductRecordDialog Vue component for create/edit mode
- [x] Implement identity and product detail sections with legacy-equivalent fields
- [x] Implement mode-aware toolbar actions: Save, Save and Close, Delete
- [x] Implement non-goal action placeholders (Attachment, Stock In/Out, Print, Export) as gated actions
- [x] Add edit-mode movement history table with running balance and audit columns

## Group 3: StockView Integration

- [x] Wire table row click and card click to open edit dialog with selected product id
- [x] Replace NEW PRODUCT placeholder with create dialog launch
- [x] Refresh StockView list after save/delete operations
- [x] Preserve existing column visibility, sort, checkbox, and view mode state behavior

## Group 4: Validation and Data Handling

- [x] Add required field validation parity (customer, category, number, product code, product name)
- [x] Add product code uniqueness checks for create and changed edit code
- [x] Implement stock number compose/parse helpers and next-number flow
- [x] Enforce read-only behavior for derived fields such as balance

## Group 5: Test Coverage

- [x] Add unit tests for ProductRecordDialog validation and mode transitions
- [x] Add StockView interaction tests for row-click edit and new-product create flows
- [ ] Add service tests for create/update/delete and uniqueness scenarios
- [x] Add or update parity tests for stock product record behavior where applicable

## Group 6: UX and Accessibility

- [x] Match legacy information architecture while using Vuetify-native interaction patterns
- [x] Add keyboard focus management for dialog open/close and confirm actions
- [ ] Verify responsive behavior for desktop/tablet/mobile breakpoints

## Group 7: Completion Gate

- [ ] Demo: edit existing product from row click
- [ ] Demo: create new product from NEW PRODUCT button
- [ ] Demo: save, save and close, delete behaviors with confirmations
- [ ] Confirm all tests pass and no regression in existing StockView features

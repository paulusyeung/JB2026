## ADDED Requirements

### Requirement: System SHALL Open Stock In/Out Entry From Stock Context Actions
The system SHALL provide a Stock In/Out transaction-entry dialog that can be launched from both the stock list context and product record context, carrying the selected product identity into the form.

#### Scenario: Launch from StockView selected row
- **WHEN** a user selects a product row in StockView and triggers the Stock In/Out action
- **THEN** the system SHALL open the Stock In/Out dialog pre-filled with that row's stock number and product identity

#### Scenario: Launch from ProductRecordDialog
- **WHEN** a user triggers the Stock In/Out action from ProductRecordDialog in edit mode
- **THEN** the system SHALL open the same Stock In/Out dialog pre-filled with the active product stock number and identity

### Requirement: Stock In/Out Form SHALL Enforce Legacy-Parity Field Rules
The Stock In/Out form SHALL capture stock number, date, reference, and quantity (+/-), and SHALL enforce parity-level validations before allowing save.

#### Scenario: Reject missing or unknown stock number
- **WHEN** the form has an empty stock number or a stock number that does not map to an existing product
- **THEN** the system SHALL block save and present a warning that stock number is required and must exist

#### Scenario: Reject invalid quantity
- **WHEN** quantity is empty or cannot be parsed as a signed integer value
- **THEN** the system SHALL block save and present a warning that quantity must be numeric and non-empty

#### Scenario: Accept signed quantity for in/out direction
- **WHEN** a user enters a positive or negative integer quantity
- **THEN** the system SHALL accept the value and interpret its sign as stock-in or stock-out direction

### Requirement: Save SHALL Persist Transaction And Update Product Balance
When save is confirmed, the system SHALL persist one stock in/out transaction row for the resolved product and SHALL update that product's balance by applying the signed quantity.

#### Scenario: Confirmed save persists transaction and balance
- **WHEN** a user confirms save on a valid Stock In/Out form
- **THEN** the system SHALL create the stock movement transaction with date, reference, qty, audit fields, and update product balance with `newBalance = oldBalance + qty`

#### Scenario: Save and close completes workflow
- **WHEN** a user confirms Save & Close on a valid Stock In/Out form
- **THEN** the system SHALL persist the transaction, close the dialog, and notify the caller to refresh stock list/movement data

#### Scenario: Cancelled confirmation performs no mutation
- **WHEN** a user cancels the save confirmation prompt
- **THEN** the system SHALL not create any transaction and SHALL not change product balance

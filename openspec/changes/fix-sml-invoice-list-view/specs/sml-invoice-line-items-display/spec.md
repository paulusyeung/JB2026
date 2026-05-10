## ADDED Requirements

### Requirement: Line items display in child table
When an invoice header row is expanded, the system SHALL display a child table containing the line items for that invoice.

#### Scenario: Child table shows line items
- **WHEN** user expands an invoice header row
- **THEN** a child table appears below the header row showing all line items for that invoice

#### Scenario: Child table shows correct columns
- **WHEN** child table is displayed
- **THEN** the following columns are visible: Line Number, Description, Quantity, Unit, Price, Amount

### Requirement: Line item data is accurate
The line item data displayed in the child table SHALL accurately reflect the data stored in the backend.

#### Scenario: Line number displays correctly
- **WHEN** child table is displayed
- **THEN** each line item's line number matches the value from the backend

#### Scenario: Description displays correctly
- **WHEN** child table is displayed
- **THEN** each line item's description matches the value from the backend

#### Scenario: Quantity displays correctly
- **WHEN** child table is displayed
- **THEN** each line item's quantity is formatted according to the user's locale settings

#### Scenario: Price displays correctly
- **WHEN** child table is displayed
- **THEN** each line item's price is formatted with 2 decimal places according to the user's locale settings

#### Scenario: Amount displays correctly
- **WHEN** child table is displayed
- **THEN** each line item's amount is formatted with 2 decimal places according to the user's locale settings

### Requirement: Empty state handling
The system SHALL handle cases where an invoice has no line items.

#### Scenario: Invoice with no line items
- **WHEN** user expands an invoice header that has no line items
- **THEN** the child table displays an empty state message (e.g., "No line items")

### Requirement: Child table styling
The child table SHALL be styled consistently with the RTF list detail panel pattern.

#### Scenario: Child table has distinct background
- **WHEN** child table is displayed
- **THEN** it has a light background color to distinguish it from the master table rows

#### Scenario: Child table row height
- **WHEN** child table is displayed
- **THEN** each row has a minimum height of 32px for proper text wrapping

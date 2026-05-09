## ADDED Requirements

### Requirement: Child table displays product line details
When an RTF header row is expanded, a child table SHALL display line item information including product code, description, price, quantity, and amount. Each line item SHALL correspond to a product or service on the RTF invoice. Line item data is already loaded with the master data and is available immediately.

#### Scenario: Line items are visible in expanded child table
- **WHEN** user expands an RTF header row
- **THEN** a child table appears showing all line items associated with that RTF header immediately (no loading delay)

#### Scenario: Line item columns are displayed with correct headers
- **WHEN** child table is displayed
- **THEN** the following columns are visible with appropriate headers: Line Number, Product Code, Product Description, Price, Quantity, Amount

#### Scenario: Numeric values are formatted correctly
- **WHEN** line items are displayed in the child table
- **THEN** price, quantity, and amount values are formatted with appropriate decimal places and thousands separators based on user locale

### Requirement: Child table column alignment and sizing
Each column in the child table SHALL have appropriate width and text alignment to ensure readability and data integrity.

#### Scenario: Line number is displayed in narrow column
- **WHEN** child table is displayed
- **THEN** the Line Number column has a width of approximately 70px

#### Scenario: Product code is left-aligned in fixed column
- **WHEN** child table is displayed
- **THEN** the Product Code column is left-aligned with a fixed width of approximately 180px

#### Scenario: Product description is left-aligned in flexible column
- **WHEN** child table is displayed
- **THEN** the Product Description column is left-aligned and occupies at least 300px width, expanding to fill available space

#### Scenario: Numeric columns are right-aligned
- **WHEN** child table is displayed
- **THEN** Price, Quantity, and Amount columns are right-aligned with appropriate fixed widths (Price: 130px, Qty: 120px, Amount: 130px)

### Requirement: Child table inherits master row context
Each child table (line items) SHALL only display items that belong to the expanded RTF header row. No cross-filtering or mixing of line items from different RTF headers SHALL occur.

#### Scenario: Child items match master row
- **WHEN** child table is displayed for a specific RTF header
- **THEN** all displayed line items are from that header's `items` array (already loaded in memory)

#### Scenario: Empty child table for RTF headers with no line items
- **WHEN** user expands an RTF header that has no associated line items
- **THEN** the child table is displayed but shows no rows (empty state)

### Requirement: Empty state messaging
If an RTF header has no line items, the child table SHALL display an appropriate empty state message.

#### Scenario: Display empty state message
- **WHEN** child table is expanded but no line items exist for that RTF header
- **THEN** an empty state message is displayed (e.g., \"No line items found\")

### Requirement: Line item data accuracy
Line items displayed in the child table SHALL match the actual data returned by the backend for that RTF invoice.

#### Scenario: Quantity matches returned value
- **WHEN** line items are displayed
- **THEN** each line's quantity value matches the quantity returned in the API response

#### Scenario: Amount is displayed correctly
- **WHEN** line items are displayed
- **THEN** each line's amount value is displayed accurately as returned from the backend

#### Scenario: All line items are displayed
- **WHEN** an RTF header has multiple line items
- **THEN** all line items from the `items` array are displayed in the child table


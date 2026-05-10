## ADDED Requirements

### Requirement: Invoice list endpoint returns line items
The `GET /api/v2/sml/invoice-list` endpoint SHALL return line item data nested within each invoice header response.

#### Scenario: Response includes items array
- **WHEN** the invoice list endpoint is called
- **THEN** each row in the response includes an `Items` property containing an array of line items

#### Scenario: Line items contain required fields
- **WHEN** line items are returned
- **THEN** each line item includes: LineNumber, Description, Quantity, Unit, Price, Amount

### Requirement: Line items are queried from database
The system SHALL query the `InvoiceItem` and `InvoiceSubItem` tables to retrieve line item data.

#### Scenario: Line items loaded for each header
- **WHEN** the invoice list endpoint is called
- **THEN** line items are loaded from the database for each invoice header in the result set

#### Scenario: Line items filtered by header
- **WHEN** line items are queried
- **THEN** only line items belonging to the invoice headers in the result set are returned

### Requirement: Response model includes items property
The `SmlInvoiceListRowResponse` model SHALL include an `Items` property of type `IReadOnlyList<SmlInvoiceListItemResponse>`.

#### Scenario: Items property exists
- **WHEN** the response model is inspected
- **THEN** it contains an `Items` property that is never null (empty array if no items)

### Requirement: Empty items array for invoices without line items
The system SHALL return an empty array for the `Items` property when an invoice has no line items.

#### Scenario: Invoice with no line items
- **WHEN** an invoice header has no associated line items in the database
- **THEN** the `Items` property is an empty array (not null)

## ADDED Requirements

### Requirement: Group Billing SHALL expose an Invoice Stats navigation entry
The system SHALL add an `Invoice Stats` menu item under Group Billing that opens a dedicated billing invoice-stats page without changing the existing Job Order > SML > Invoice Stats entry.

#### Scenario: User opens billing invoice stats page from the menu
- **WHEN** an authorized user expands Group Billing navigation
- **THEN** the menu includes `Invoice Stats` and routes to the billing invoice-stats page

### Requirement: Group Billing Invoice Stat SHALL use billing invoice summary data
The system SHALL populate the billing invoice-stats page from Invoice Ninja-backed billing invoice summaries exposed by the existing JB2026 billing API, default to current-year invoices in `Sent` status when no date filters are supplied, and SHALL NOT require a new backend aggregation endpoint for the initial implementation.

#### Scenario: Billing invoice stats page loads data
- **WHEN** the user opens Group Billing > Invoice Stats
- **THEN** the page requests the current JB2026 billing invoice summary list, which is backed by Invoice Ninja, and uses current-year `Sent` invoices from that response as the source dataset when no date range is set

#### Scenario: Billing invoice stats page filters by selected dates
- **WHEN** the user sets a start date, an end date, or both and runs search
- **THEN** the page uses `Sent` invoices whose invoice date falls within the selected date range as the source dataset

### Requirement: Group Billing Invoice Stat SHALL expose the requested field set
The system SHALL map billing invoice summaries into the fields `CustomerName`, `InvoiceNumber`, `InvoiceDate`, `InvoiceAmount`, `Year`, and `Month`, where `Year` and `Month` are derived from `InvoiceDate`.

#### Scenario: Billing invoice summary row is transformed for the pivot
- **WHEN** a billing invoice summary includes customer, invoice number, invoice date, and amount
- **THEN** the page produces one pivot row containing those fields plus `Year` and `Month` derived from the invoice date

### Requirement: Group Billing Invoice Stat SHALL provide date range filters without lookup controls
The system SHALL render the billing invoice-stat page with optional start-date and end-date filters, and SHALL NOT provide lookup or text-search filters.

#### Scenario: User views the billing invoice stat toolbar area
- **WHEN** the billing invoice-stat page is displayed
- **THEN** the page shows start-date and end-date inputs and does not show a lookup or text-search filter input

### Requirement: Group Billing Invoice Stat SHALL apply the requested default pivot layout
The system SHALL initialize the billing invoice-stat view with rows `CustomerName`, columns `Year` and `Month`, and values `InvoiceAmount` aggregated by `SUM`.

#### Scenario: Billing invoice stat pivot is initialized
- **WHEN** the billing invoice-stat dataset has loaded successfully
- **THEN** the pivot defaults to rows `CustomerName`, columns `Year` and `Month`, and summed `InvoiceAmount` values

### Requirement: Group Billing Invoice Stat SHALL use a consistent unknown-date label
The system SHALL map missing or invalid `InvoiceDate` values to a consistent `Unknown` label for derived `Year` and `Month` fields instead of leaving empty pivot buckets.

#### Scenario: Invoice date is missing
- **WHEN** a billing invoice summary row does not contain a usable invoice date
- **THEN** the derived `Year` and `Month` values for that row are labeled `Unknown`

### Requirement: Group Billing Invoice Stat SHALL preserve existing invoice-stats behavior
The system SHALL introduce the billing invoice-stat page as an additional surface and SHALL NOT change the current Job Order > SML > Invoice Stats route, filters, or layout.

#### Scenario: Existing SML invoice stats page is opened after the billing change
- **WHEN** a user navigates to Job Order > SML > Invoice Stats
- **THEN** the existing SML invoice stats experience behaves as it did before the billing invoice-stat feature was added
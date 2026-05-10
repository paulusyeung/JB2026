## MODIFIED Requirements

### Requirement: Master table displays invoice header information
The invoice list page SHALL display a master table with invoice header information in columnar format. Each row represents one invoice header with consolidated header metadata. The table SHALL support expandable rows to show line item details.

#### Scenario: Master table shows all invoice headers
- **WHEN** user loads the SML Invoice List View page
- **THEN** system displays a table with one row per invoice header in the system

#### Scenario: Master table columns are displayed with correct headers
- **WHEN** master table is displayed
- **THEN** the following columns are visible: Invoice Number, Row Number, Customer Name, Invoice Date, Invoice Amount, IC Number, Created On, Created By

#### Scenario: Master table includes expansion indicator
- **WHEN** master table is displayed
- **THEN** each row includes an expand icon to allow expansion to view line items

### Requirement: Master table column values are accurate
Data displayed in each master table column SHALL accurately reflect the invoice header information stored in the backend.

#### Scenario: Invoice Number column displays correct value
- **WHEN** master table is displayed
- **THEN** each row's Invoice Number column shows the invoice number

#### Scenario: Invoice Date is formatted according to user locale
- **WHEN** master table is displayed
- **THEN** Invoice Date values are formatted according to the user's locale settings

#### Scenario: Invoice Amount is formatted correctly
- **WHEN** master table is displayed
- **THEN** Invoice Amount values are formatted with 2 decimal places according to the user's locale settings

### Requirement: Master table supports filtering and search
Users SHALL be able to search and filter the invoice list by lookup criteria and common query options.

#### Scenario: Lookup field filters by invoice details
- **WHEN** user enters text in the lookup field and clicks Search
- **THEN** system filters master table to show only rows matching the lookup text in invoice number, customer name, or IC number

#### Scenario: Common query dropdown filters by date range
- **WHEN** user selects a common query option (e.g., Last 30 days, Last 60 days, Last 90 days, All)
- **THEN** system filters master table to show rows based on the selected time period

#### Scenario: Search with empty lookup field applies common query only
- **WHEN** user leaves the lookup field empty and clicks Search
- **THEN** system applies the selected common query filter and ignores the lookup

#### Scenario: Lookup takes precedence over common query
- **WHEN** user enters both lookup text and selects a common query
- **THEN** system applies only the lookup filter and ignores the common query

### Requirement: Master table supports refresh
Users SHALL be able to manually refresh the master table data from the backend.

#### Scenario: Refresh button reloads data
- **WHEN** user clicks the Refresh button
- **THEN** system reloads the master table data from the backend and displays the updated list

#### Scenario: Refresh button shows loading state
- **WHEN** user clicks Refresh button
- **THEN** button displays a loading spinner and is disabled until data refresh completes

#### Scenario: Refresh clears expanded rows
- **WHEN** user clicks Refresh button with rows expanded
- **THEN** all expanded rows are collapsed after the data refresh completes

### Requirement: Master table handles empty and error states
The system SHALL gracefully handle cases where no data is available or errors occur during data loading.

#### Scenario: Empty state when no results match filters
- **WHEN** user applies filters that result in no matching invoice headers
- **THEN** master table displays an empty state message (e.g., "No invoices found")

#### Scenario: Error message when data load fails
- **WHEN** backend API returns an error while loading master table data
- **THEN** an error message is displayed and user can retry by clicking Refresh

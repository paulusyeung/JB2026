## MODIFIED Requirements

### Requirement: Master table displays RTF header information
The RTF invoice list page SHALL display a master table with RTF header information in columnar format. Each row represents one RTF (RFQ turned into purchase order) invoice header with consolidated header metadata.

#### Scenario: Master table shows all RTF headers
- **WHEN** user loads the SML RTF List View page
- **THEN** system displays a table with one row per RTF header in the system

#### Scenario: Master table columns are displayed with correct headers
- **WHEN** master table is displayed
- **THEN** the following columns are visible: Purchase Order, Row Number, Customer PO, Ordered By, Ordered On, Original PO, Sales Order, Original SO, DN Count, Invoice Number, Created On, Created By

#### Scenario: Master table includes row number and expansion indicator
- **WHEN** master table is displayed
- **THEN** each row includes a row number and an expand arrow icon to allow expansion to view line items

### Requirement: Master table displays DNCount column
The master table SHALL display the DN Count column showing the number of delivery notes associated with each RTF header.

#### Scenario: DN Count column is visible
- **WHEN** master table is displayed
- **THEN** a DN Count column is visible showing the count of delivery notes for each header

#### Scenario: DN Count displays correct value
- **WHEN** master table is displayed
- **THEN** each row's DN Count column shows the value from `dnCount` in the API response

### Requirement: Master table column values are accurate
Data displayed in each master table column SHALL accurately reflect the RTF header information stored in the backend.

#### Scenario: Purchase Order column displays correct value
- **WHEN** master table is displayed
- **THEN** each row's Purchase Order column shows the RTF purchase order number

#### Scenario: Ordered On is formatted according to user locale
- **WHEN** master table is displayed
- **THEN** Ordered On values are formatted according to the user's locale settings

#### Scenario: Created On timestamp includes date and time
- **WHEN** master table is displayed
- **THEN** Created On column shows both date and time in user's locale format

### Requirement: Master table supports filtering and search
Users SHALL be able to search and filter the RTF invoice list by lookup criteria and common query options.

#### Scenario: Lookup field filters by purchase order
- **WHEN** user enters text in the lookup field and clicks Search
- **THEN** system filters master table to show only rows matching the lookup text in purchase order, customer PO, original PO, sales order, or original SO

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
- **WHEN** user applies filters that result in no matching RTF headers
- **THEN** master table displays an empty state message (e.g., \"No invoices found\")

#### Scenario: Error message when data load fails
- **WHEN** backend API returns an error while loading master table data
- **THEN** an error message is displayed and user can retry by clicking Refresh

### Requirement: Master table limit
The system SHALL limit the number of rows returned from the backend to prevent memory and performance issues.


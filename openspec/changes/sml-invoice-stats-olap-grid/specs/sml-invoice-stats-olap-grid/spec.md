## ADDED Requirements

### Requirement: Invoice Stats SHALL render using WebPivotTable
The system SHALL render SML Invoice Stats with the WebPivotTable OLAP grid component instead of a custom HTML pivot table.

#### Scenario: Invoice Stats page loads OLAP grid
- **WHEN** a user opens Job Order > SML > Invoice Stats
- **THEN** the page initializes and displays the WebPivotTable component container

### Requirement: Invoice Stats SHALL use existing invoice-stats data source
The system SHALL source Invoice Stats OLAP data from the existing invoice-stats API using the active filter inputs (start date, end date, lookup, take) without introducing a new endpoint contract.

#### Scenario: User applies filters
- **WHEN** the user executes search/refresh with date or lookup filters
- **THEN** the frontend requests the existing invoice-stats endpoint and refreshes the OLAP dataset from that response

### Requirement: Invoice Stats SHALL apply legacy default OLAP layout
The system SHALL initialize WebPivotTable with legacy-equivalent default dimensions and measures: row fields CustomerName, InvoiceNumber, PurchaseOrder, ProductCode, Qty, Unit, Price; column fields Year and Month; data field Amount with totals visible.

#### Scenario: Default layout appears on first render
- **WHEN** the grid is initialized with a valid dataset
- **THEN** the default row, column, and data fields match the legacy Invoice Stats layout configuration

### Requirement: Invoice Stats SHALL transform API rows into valid OLAP tabular input
The system SHALL convert invoice-stats response rows into the WebPivotTable-compatible tabular format required by the component initialization API.

#### Scenario: Data transformation completes successfully
- **WHEN** the API returns one or more invoice-stats rows
- **THEN** the frontend constructs a valid header definition and row matrix payload accepted by WebPivotTable initialization

### Requirement: Invoice Stats SHALL provide robust UI states around OLAP initialization
The system SHALL provide loading, empty, and error states if data retrieval fails or OLAP initialization fails, without crashing the page.

#### Scenario: Initialization failure is handled
- **WHEN** WebPivotTable initialization throws an error for a response
- **THEN** the page displays a user-visible warning state and keeps filter controls available for retry

### Requirement: ClientApp SHALL install and resolve WebPivotTable locally
The system SHALL include WebPivotTable as a local ClientApp dependency and resolve its runtime assets in local development and build execution.

#### Scenario: Local dependency is available
- **WHEN** developers install dependencies and run the ClientApp locally
- **THEN** WebPivotTable is present in node_modules and can be imported by the Invoice Stats view without missing-module failure

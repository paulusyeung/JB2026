## ADDED Requirements

### Requirement: Invoice Stats SHALL render using WebPivotTable
The system SHALL render SML Invoice Stats with the WebPivotTable OLAP grid component instead of a custom HTML pivot table.

#### Scenario: Invoice Stats page loads OLAP grid
- **WHEN** a user opens Job Order > SML > Invoice Stats
- **THEN** the page initializes and displays the WebPivotTable component container

### Requirement: Invoice Stats SHALL use existing invoice-stats data source
The system SHALL source Invoice Stats OLAP data from the existing invoice-stats API using the active filter inputs (start date, end date, lookup) without introducing a new endpoint contract.

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

### Requirement: Invoice Stats SHALL use correct WebPivotTable initialization signature
The system SHALL initialize WebPivotTable tabular data with the explicit two-argument signature (`attrArray`, `dataArray`) and SHALL NOT rely on ambiguous single-payload calls.

#### Scenario: Hydration call uses explicit header and row matrix
- **WHEN** Invoice Stats hydrates OLAP data into WebPivotTable
- **THEN** it passes a field-name array and a 2D row matrix as separate arguments to the initialization API

### Requirement: Invoice Stats SHALL synchronize OLAP hydration with component readiness
The system SHALL wait for WebPivotTable custom-element readiness and method availability before attempting data hydration or layout configuration.

#### Scenario: Component not yet ready at first render
- **WHEN** the Invoice Stats view mounts before WebPivotTable APIs are available
- **THEN** the page retries hydration safely and does not fail into a persistent blank-grid state

### Requirement: Invoice Stats SHALL render WebPivotTable host with explicit visible sizing
The system SHALL render the WebPivotTable host as a block-level element with explicit height constraints to prevent inline-element clipping.

#### Scenario: OLAP grid host layout is applied
- **WHEN** the Invoice Stats page renders the WebPivotTable host
- **THEN** the host occupies visible vertical space and the pivot content is not clipped at the top

### Requirement: Invoice Stats SHALL provide robust UI states around OLAP initialization
The system SHALL provide loading, empty, and error states if data retrieval fails or OLAP initialization fails, without crashing the page.

#### Scenario: Initialization failure is handled
- **WHEN** WebPivotTable initialization throws an error for a response
- **THEN** the page displays a user-visible warning state and keeps filter controls available for retry

### Requirement: Invoice Stats SHALL default to OLAP grid mode and reporting-friendly Amount formatting
The system SHALL initialize WebPivotTable in grid mode and SHALL render Amount values with thousands separators and two decimal places.

#### Scenario: Default Invoice Stats view is opened
- **WHEN** the OLAP layout is configured after successful hydration
- **THEN** display mode is grid and Amount aggregations appear with 2 decimal places and thousand-delimited numeric formatting

### Requirement: ClientApp SHALL install and resolve WebPivotTable locally
The system SHALL include WebPivotTable as a local ClientApp dependency and resolve its runtime assets in local development and build execution.

#### Scenario: Local dependency is available
- **WHEN** developers install dependencies and run the ClientApp locally
- **THEN** WebPivotTable is present in node_modules and can be imported by the Invoice Stats view without missing-module failure

## ADDED Requirements

### Requirement: Job stats SHALL be computed from the JobOrder table
The job stats endpoint `GET /api/v2/job-orders/stats` SHALL derive its rows from the `JobOrder` table (querying `JobOrders` on the read context) rather than from the legacy SQL views `vwJobStatCoGS` / `vwJobStatGrossProfit`.

#### Scenario: Rows reflect job orders with a non-zero invoice amount
- **GIVEN** a read context with seeded `JobOrder` records that have `InvoiceAmount` set to `0` and others set to a non-zero value
- **WHEN** `EfJobManagementRepository.GetJobStats` is executed
- **THEN** the result SHALL contain one row per job order whose `InvoiceAmount` is non-zero
- **AND** the result SHALL exclude job orders whose `InvoiceAmount` is `0` or NULL

#### Scenario: Fields are derived with legacy semantics
- **GIVEN** a job order with `OrderNumber`, `JobNumber`, `OrderTitle`, `CustomerRef`, `OrderedBy`, `InvoiceRef`, and `CompletedOn` set
- **WHEN** `GetJobStats` returns the row for that job order
- **THEN** `JobNumber` SHALL be the composite `OrderNumber-JobNumber`
- **AND** `Brand` SHALL equal `OrderTitle`
- **AND** `PurchaseOrder` SHALL equal `CustomerRef`
- **AND** `SalesRep` SHALL equal `OrderedBy`
- **AND** `InvNumber` SHALL equal `InvoiceRef`

#### Scenario: Invoice date follows the completed/required fallback
- **GIVEN** a job order whose `CompletedOn` year is NOT `1900`
- **WHEN** `GetJobStats` returns the row
- **THEN** `InvDate` SHALL equal `CompletedOn`
- **GIVEN** a job order whose `CompletedOn` year IS `1900`
- **WHEN** `GetJobStats` returns the row
- **THEN** `InvDate` SHALL equal `RequiredOn`

### Requirement: Job stats Cost SHALL be derived from OriginalSONumber
The `Cost` value in each stats row SHALL be derived from the job order's `OriginalSONumber` using the numeric-guard semantics the legacy view applied to `ProductStyle`: only values consisting entirely of digits and a single optional decimal point are treated as numeric and parsed as `DECIMAL(10,4)`; NULL, empty, or any non-numeric value SHALL produce a cost of `0`.

#### Scenario: Numeric OriginalSONumber parses as cost
- **GIVEN** a job order with `OriginalSONumber = "1234.5"`
- **WHEN** `GetJobStats` returns the row
- **THEN** `Cost` SHALL equal `1234.5`

#### Scenario: NULL or empty OriginalSONumber yields zero cost
- **GIVEN** a job order whose `OriginalSONumber` is NULL
- **WHEN** `GetJobStats` returns the row
- **THEN** `Cost` SHALL equal `0`
- **GIVEN** a job order whose `OriginalSONumber` is an empty string
- **WHEN** `GetJobStats` returns the row
- **THEN** `Cost` SHALL equal `0`

#### Scenario: Non-numeric OriginalSONumber yields zero cost
- **GIVEN** a job order whose `OriginalSONumber` contains non-numeric characters (e.g., `"PO-123"` or `"12.34.56"`)
- **WHEN** `GetJobStats` returns the row
- **THEN** `Cost` SHALL equal `0`

### Requirement: Job stats date-range filtering SHALL apply to the derived invoice date
The `startOn` and `endOn` query parameters SHALL filter rows on the derived `InvDate`, preserving the existing boundary behavior: `startOn` counts from the start of its day (inclusive), and `endOn` includes the full `endOn` day (rows must be strictly before the day after `endOn`).

#### Scenario: startOn filters inclusively
- **GIVEN** `GetJobStats(startOn = 2026-01-15)`
- **WHEN** rows are returned
- **THEN** every row SHALL have `InvDate >= 2026-01-15` (i.e., on or after the start of that day)

#### Scenario: endOn includes the full end day
- **GIVEN** `GetJobStats(endOn = 2026-03-01)`
- **WHEN** rows are returned
- **THEN** every row SHALL have `InvDate < 2026-03-02`
- **AND** rows invoiced on `2026-03-01` SHALL be included

### Requirement: Job stats response SHALL remain backward compatible
The endpoint SHALL keep returning `IReadOnlyList<JobStatsResponse>` with the same fields and the same `GrossProfit` percentage semantics (computed as `ROUND((InvoiceAmount - Cost) / InvoiceAmount * 100, 2)`), so existing client consumers (the pivot table in `JobStatsView.vue`, CSV export) do not change.

#### Scenario: GrossProfit preserves percentage semantics
- **GIVEN** a row with `InvoiceAmount = 200` and `Cost = 50`
- **WHEN** `GetJobStats` returns the row
- **THEN** `GrossProfit` SHALL equal `75.00` (i.e., `(200 - 50) / 200 * 100`)

#### Scenario: Rows are ordered by invoice date then invoice number
- **WHEN** `GetJobStats` returns multiple rows
- **THEN** the rows SHALL be ordered ascending by `InvDate`, then by `InvNumber`

### Requirement: In-memory repository SHALL mirror cost derivation
`InMemoryJobManagementRepository.GetJobStats` SHALL derive `Cost` from the stored `OriginalSONumber` using the same numeric-guard semantics as the EF repository so parity with the EF implementation is preserved.

#### Scenario: In-memory cost uses OriginalSONumber
- **GIVEN** an in-memory job whose `OriginalSONumber` is a numeric string
- **WHEN** `GetJobStats` is executed against the in-memory repository
- **THEN** `Cost` SHALL equal the parsed value of `OriginalSONumber`
- **AND** non-numeric, NULL, or empty `OriginalSONumber` SHALL yield `Cost = 0`
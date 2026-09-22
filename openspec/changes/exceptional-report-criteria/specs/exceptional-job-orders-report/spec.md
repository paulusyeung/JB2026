## MODIFIED Requirements

### Requirement: Exceptional report displays job orders within a date range
When a user opens the exceptional report at `/job-order/reports/exceptional`, they SHALL be able to select a date range (start date + end date) and see job orders falling within that range that match at least one enabled exceptional criterion.

#### Scenario: Successful query returns job orders
- **WHEN** the user selects Start Date `2025-01-01` and End Date `2025-01-31`
- **AND** clicks refresh
- **THEN** the system displays job orders with `orderedOn` falling within that range that match at least one enabled exceptional criterion

#### Scenario: Empty result when no jobs match
- **WHEN** the user selects a date range where no job orders exist
- **THEN** the system shows an empty table with a clear message

#### Scenario: Empty result when no criteria match
- **WHEN** the user selects a date range containing job orders
- **AND** none of those job orders match an enabled exceptional criterion
- **THEN** the system shows an empty table with a clear message

#### Scenario: Summary chips shown
- **WHEN** the report loads with results
- **THEN** the view shows total row count and total invoice amount in chip indicators
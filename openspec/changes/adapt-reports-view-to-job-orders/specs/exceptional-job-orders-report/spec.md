## CHANGED Requirements

### Requirement: Exceptional report displays job orders within a date range
When a user opens the exceptional report at `/job-order/reports/exceptional`, they SHALL be able to select a date range (start date + end date) and see job orders falling within that range.

#### Scenario: Successful query returns job orders
- **WHEN** the user selects Start Date `2025-01-01` and End Date `2025-01-31`
- **AND** clicks refresh
- **THEN** the system displays job orders with `orderedOn` falling within that range

#### Scenario: Empty result when no jobs match
- **WHEN** the user selects a date range where no job orders exist
- **THEN** the system shows an empty table with a clear message

#### Scenario: Summary chips shown
- **WHEN** the report loads with results
- **THEN** the view shows total row count and total invoice amount in chip indicators

### Requirement: Date range replaces month picker
The view SHALL use two separate date inputs (start date, end date) instead of a single month picker.

#### Scenario: Default range is current month
- **WHEN** the user first opens the view
- **THEN** start date defaults to the first day of the current month and end date to the last day

### Requirement: All existing ExceptionalReportView features preserved
The merged view SHALL retain: column picker, sorting (asc/desc), card/detail toggle, checkbox selection mode, job editor dialog, print manager dialog, and invoice summary hydration.

### Requirement: Old reports path removed
The old `/reports` route and its quotation-based endpoint SHALL be removed.

#### Scenario: Navigating to /reports
- **WHEN** a user navigates to `/reports`
- **THEN** they SHALL be redirected to `/dashboard` (or the route is removed entirely)

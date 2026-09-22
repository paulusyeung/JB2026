## Purpose

Defines the selection criteria and day thresholds that determine which job orders the Exceptional Report treats as exceptional, and how users can configure them.

## ADDED Requirements

### Requirement: Exceptional criteria are user-configurable
The view SHALL let the user enable or disable each exceptional criterion and set a day threshold for the criteria that use one. The selection SHALL persist per user across sessions.

#### Scenario: User enables and disables criteria
- **WHEN** the user toggles an exceptional criterion on
- **THEN** the report includes job orders matching that criterion
- **AND** when the user toggles it off, the report excludes rows matched only by that criterion

#### Scenario: User changes a day threshold
- **WHEN** the user changes the day threshold of a criterion
- **THEN** the report re-evaluates matching rows using the new threshold without reloading from the server

#### Scenario: Criteria persist
- **WHEN** the user reloads the view
- **THEN** the enabled criteria and thresholds retain the previously saved selection

### Requirement: A job order is exceptional when it matches an enabled criterion
A job order SHALL be included in the report when it matches at least one enabled criterion. Criteria are combined with OR.

#### Scenario: Job matches exactly one criterion
- **WHEN** a job order matches one enabled criterion and no others
- **THEN** the job is included in the report

#### Scenario: Job matches several criteria
- **WHEN** a job order matches several enabled criteria
- **THEN** the job is included in the report
- **AND** the view shows which criteria the job matched

#### Scenario: Job matches no enabled criterion
- **WHEN** a job order matches none of the enabled criteria
- **THEN** the job is excluded from the report

### Requirement: Long-duration criteria use a day threshold from the ordered date
Each "for a long period of time" criterion SHALL count days from the job's `orderedOn` date, and the threshold SHALL be a whole number of days selectable by the user.

#### Scenario: Not scheduled for a long period
- **WHEN** a job has no non-cancelled schedule
- **AND** the days since it was ordered exceed the configured threshold
- **THEN** the "not scheduled" criterion matches the job

#### Scenario: Not scheduled but recently ordered
- **WHEN** a job has no non-cancelled schedule
- **BUT** the days since it was ordered do not exceed the configured threshold
- **THEN** the "not scheduled" criterion does not match the job

#### Scenario: Not completed for a long period
- **WHEN** a job has no completion date
- **AND** the days since it was ordered exceed the configured threshold
- **THEN** the "not completed" criterion matches the job

#### Scenario: No invoice for a long period
- **WHEN** a job has no invoice number
- **AND** the days since it was ordered exceed the configured threshold
- **THEN** the "no invoice" criterion matches the job

#### Scenario: No COGS for a long period
- **WHEN** a job has no COGS value recorded (the `OriginalSONumber` field, labeled COGS)
- **AND** the days since it was ordered exceed the configured threshold
- **THEN** the "no COGS" criterion matches the job

### Requirement: No invoice after completion
A job that has a completion date but no invoice number SHALL match the "no invoice after completion" criterion regardless of any day threshold.

#### Scenario: Completed job without invoice
- **WHEN** a job has a completion date
- **AND** the job has no invoice number
- **THEN** the "no invoice after completion" criterion matches the job

#### Scenario: Completed job with invoice
- **WHEN** a job has a completion date
- **AND** the job has an invoice number
- **THEN** the "no invoice after completion" criterion does not match the job
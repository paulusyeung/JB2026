## ADDED Requirements

### Requirement: Data Correctness Tests Must Cover All Critical Entity Read and Write Paths
Automated data correctness tests SHALL cover create, read, update, and delete paths for all business-critical entities, validating that EF Core 8 behaviour matches EF6 baseline.

#### Scenario: Data correctness test passes for critical entity CRUD
- **WHEN** the data correctness test suite is run
- **THEN** all assertions comparing EF Core 8 output to EF6 baseline output SHALL pass

#### Scenario: Data correctness tests run in CI
- **WHEN** the CI pipeline runs for data layer changes
- **THEN** the data correctness test suite SHALL execute automatically and block merge on failure

### Requirement: Transaction and Concurrency Behaviour Must Be Validated
All entity types with optimistic concurrency tokens SHALL have explicit concurrency conflict test scenarios.

#### Scenario: Concurrency conflict is detected and reported correctly
- **WHEN** two concurrent updates are attempted to the same entity row
- **THEN** the second update SHALL throw a `DbUpdateConcurrencyException` as expected

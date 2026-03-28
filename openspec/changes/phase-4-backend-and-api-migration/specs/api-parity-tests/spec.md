## ADDED Requirements

### Requirement: Every Migrated Slice Must Have Automated Parity Tests
Each migrated API domain slice MUST have automated parity tests that compare response body, status code, and key headers against a captured legacy baseline snapshot.

#### Scenario: Parity tests run in CI without manual steps
- **WHEN** CI runs for a migrated slice branch
- **THEN** all parity tests SHALL execute automatically and report pass or fail

#### Scenario: Parity test failure blocks merge
- **WHEN** a parity test fails in CI
- **THEN** the pull request SHALL be blocked from merging

### Requirement: Legacy Baseline Snapshot Must Be Captured Before Migration
A legacy response snapshot MUST be captured for each endpoint before migration begins and stored as the parity test reference.

#### Scenario: Snapshot exists before slice migration starts
- **WHEN** a slice migration pull request is opened
- **THEN** a corresponding legacy snapshot file SHALL already exist in the test fixtures

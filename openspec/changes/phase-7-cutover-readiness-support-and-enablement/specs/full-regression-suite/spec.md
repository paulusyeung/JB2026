## ADDED Requirements

### Requirement: All API and UI Regression Tests Must Pass Before Cutover Is Approved
The full regression test suite (unit, integration, and Playwright E2E) MUST be executed and produce a green result before the go/no-go checklist can be signed.

#### Scenario: Regression suite is green before go/no-go meeting
- **WHEN** the regression test suite is run against the staging environment
- **THEN** 100% of tests SHALL pass and the suite exit code SHALL be 0

### Requirement: Regression Suite Must Cover All Migrated API Endpoints and UI Slices
Every API endpoint migrated in Phase 4 and every UI slice migrated in Phase 6 MUST have at least one regression test scenario in the consolidated suite.

#### Scenario: Coverage report confirms all migrated endpoints are tested
- **WHEN** a coverage map of migrated API routes vs. regression test coverage is generated
- **THEN** zero migrated routes SHALL have no corresponding regression scenario

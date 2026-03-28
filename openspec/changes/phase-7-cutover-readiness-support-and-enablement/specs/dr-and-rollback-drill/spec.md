## ADDED Requirements

### Requirement: A Rollback Procedure Must Be Documented and Timed in Staging
A written rollback runbook MUST exist and be executed end-to-end in a production-equivalent staging environment. The measured time-to-rollback MUST be ≤ 15 minutes.

#### Scenario: Rollback drill completes within time target
- **WHEN** the rollback runbook is executed in staging from the point of simulated cutover failure
- **THEN** production traffic SHALL be fully returned to the legacy JB2015 system within 15 minutes and all legacy routes SHALL respond correctly

### Requirement: A Disaster Recovery Drill Must Validate the RTO for Full Application Restore
A DR drill MUST be conducted in staging by simulating primary database failure. The application MUST be restored and serving traffic within the agreed RTO.

#### Scenario: DR drill meets RTO target
- **WHEN** the primary database is taken offline in staging and the DR procedure is executed
- **THEN** the application SHALL be restored from backup and serving valid responses within the agreed RTO

### Requirement: Rollback and DR Runbooks Must Be Stored in the Repository
Both the rollback runbook and the DR procedure MUST be committed to the repository as Markdown documents and reviewed before the go/no-go meeting.

#### Scenario: Runbooks are present and up to date in the repository
- **WHEN** the go/no-go checklist is reviewed
- **THEN** the rollback and DR runbooks SHALL be present in `docs/runbooks/` and SHALL have been updated within the current sprint

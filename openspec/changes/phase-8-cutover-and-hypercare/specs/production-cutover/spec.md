## ADDED Requirements

### Requirement: Production Cutover Must Be Executed According to the Approved Runbook
The production cutover MUST follow the runbook produced and rehearsed in Phase 7. No ad-hoc steps are permitted during the cutover window.

#### Scenario: Each cutover step is checked off as it is completed
- **WHEN** the cutover runbook is being executed
- **THEN** each step SHALL be acknowledged by the executing operator in the live incident/change record before proceeding to the next step

### Requirement: Post-Flip Smoke Tests Must Pass Before Exiting the Maintenance Window
After the traffic flip, a defined set of production smoke tests MUST pass before the maintenance page is removed and users are admitted.

#### Scenario: Smoke tests pass before maintenance window closes
- **WHEN** the load balancer flip to JB2026 is complete
- **THEN** the automated smoke test suite SHALL be run against the production endpoint and all tests SHALL pass before the maintenance window is exited

### Requirement: Rollback Must Remain Available for at Least 72 Hours Post-Flip
The JB2015 application slot MUST remain warm and accessible via load-balancer reconfiguration for at least 72 hours after the successful cutover.

#### Scenario: Rollback can be executed within 15 minutes within the 72-hour window
- **WHEN** a P1 incident requires rollback within 72 hours of cutover
- **THEN** the traffic flip to JB2015 SHALL be executable and complete within 15 minutes

## ADDED Requirements

### Requirement: p95 API Latency Must Not Exceed 500 ms Under 2× Expected Peak Load
Under a load scenario representing twice the expected production peak concurrent user count, the p95 response latency for all critical API endpoints MUST be ≤ 500 ms.

#### Scenario: Load test passes p95 latency gate
- **WHEN** the k6 peak load scenario is executed at 2× expected concurrent users
- **THEN** the p95 response latency for all critical endpoints SHALL be ≤ 500 ms and the error rate SHALL be < 1%

### Requirement: Load Tests Must Be Defined as Code in the Repository
All load test scripts MUST be stored as k6 JavaScript files in the repository and runnable from CI without manual configuration.

#### Scenario: Load test scripts run successfully in CI
- **WHEN** the CI pipeline triggers the load test job
- **THEN** the k6 scripts SHALL execute without configuration errors and produce a results summary artefact

### Requirement: A Soak Test Must Validate Stability Over 30 Minutes at Sustained Load
A soak test MUST run at expected peak load for a minimum of 30 minutes without memory growth exceeding 20% of baseline or error rate rising above 1%.

#### Scenario: Soak test completes within stability thresholds
- **WHEN** the k6 soak scenario runs for 30 minutes at peak load
- **THEN** heap memory SHALL not grow more than 20% over baseline and error rate SHALL remain below 1%

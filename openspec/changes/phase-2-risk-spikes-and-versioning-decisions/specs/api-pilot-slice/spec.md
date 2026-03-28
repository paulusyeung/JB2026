## ADDED Requirements

### Requirement: API Pilot Slice Must Deliver a Production-Quality Endpoint
The team SHALL migrate one medium-complexity legacy Web API 2 endpoint to ASP.NET Core with full parity tests and logging.

#### Scenario: Pilot endpoint returns identical response to legacy endpoint
- **WHEN** the pilot endpoint is called with the same request payload as the legacy endpoint
- **THEN** the response body, status code, and headers SHALL match the legacy baseline snapshot

#### Scenario: Pilot endpoint passes security checks
- **WHEN** the pilot endpoint is reviewed for security
- **THEN** auth, input validation, CORS, and error response format SHALL all comply with ASP.NET Core target standards

### Requirement: Pilot Slice Must Include Parity Tests
The pilot MUST include automated parity tests that run in CI and run against both the legacy and new endpoint.

#### Scenario: Parity tests run in CI without manual steps
- **WHEN** the CI pipeline runs for the pilot slice
- **THEN** all parity tests SHALL execute automatically and report pass or fail

### Requirement: Pilot Slice Serves as Migration Blueprint
The pilot implementation MUST be documented as the canonical pattern for all subsequent API migration slices.

#### Scenario: Blueprint documentation is available before Phase 3 begins
- **WHEN** Phase 3 API migration planning starts
- **THEN** a migration blueprint document SHALL exist referencing the pilot implementation as the reference pattern

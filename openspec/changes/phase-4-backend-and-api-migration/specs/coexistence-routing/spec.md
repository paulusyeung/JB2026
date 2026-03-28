## ADDED Requirements

### Requirement: Legacy and New API Endpoints Must Coexist During Migration
The routing model SHALL allow legacy and new endpoints to operate simultaneously so each domain slice can be independently migrated, verified, and cut over.

#### Scenario: New slice endpoint is accessible while legacy endpoint still responds
- **WHEN** a domain slice is in mid-migration
- **THEN** both the legacy endpoint and the new endpoint SHALL be reachable and independently testable

### Requirement: Legacy Route Is Disabled Only After Parity Verification and UAT Sign-Off
A legacy API route SHALL only be retired after parity tests pass in CI and product owner UAT sign-off is recorded.

#### Scenario: Route retirement requires sign-off
- **WHEN** a legacy route is proposed for retirement
- **THEN** evidence of passing parity tests and UAT sign-off SHALL be required before the route is disabled

## ADDED Requirements

### Requirement: Auth Spike Must Produce an Approved Target Architecture
The team SHALL produce a documented auth/session architecture decision for ASP.NET Core that covers token strategy, session handling, and CORS policy.

#### Scenario: Architecture decision is reviewed and approved
- **WHEN** the auth spike output is reviewed at Gate A/B
- **THEN** a single approved auth approach SHALL be recorded with documented rationale and trade-offs

### Requirement: Auth Architecture Must Be Compatible With Vue 3 SPA Consumption
The chosen auth approach SHALL support consumption by the Vue 3 front-end without requiring proprietary session middleware.

#### Scenario: Vue 3 client can authenticate using approved flow
- **WHEN** the spike auth flow is implemented
- **THEN** the Vue 3 API client SHALL be able to obtain, use, and refresh credentials without OWIN or proprietary session dependencies

### Requirement: Auth Architecture Must Use OSS-Compatible Middleware Only
Authentication and authorisation middleware MUST NOT reference Thinktecture IdentityModel legacy packages or OWIN/Katana.

#### Scenario: Auth middleware dependencies pass license check
- **WHEN** auth middleware dependencies are reviewed
- **THEN** all packages SHALL have OSS or .NET Foundation redistribution-compatible licenses

## ADDED Requirements

### Requirement: Feature Flags Must Allow Per-Slice Legacy/Vue 3 Routing Coexistence
A feature flag mechanism MUST allow each migrated UI slice to be enabled or disabled independently without a code deployment. When a flag is disabled, the original WebForms route MUST serve the request.

#### Scenario: Disabled flag falls back to legacy WebForms route
- **WHEN** the feature flag for a given UI slice is set to `disabled`
- **THEN** requests to that route SHALL be forwarded to the legacy WebForms handler without error

#### Scenario: Enabled flag routes to Vue 3 SPA
- **WHEN** the feature flag for a given UI slice is set to `enabled`
- **THEN** requests to that route SHALL be served by the Vue 3 SPA entry point

### Requirement: Feature Flag Changes Must Take Effect Without Application Restart
Feature flag values MUST be read from a persistent store (database or config file) and applied within 60 seconds of change without restarting the application process.

#### Scenario: Flag is toggled and takes effect within TTL
- **WHEN** a feature flag value is changed in the flag store
- **THEN** subsequent requests AFTER the cache TTL (≤ 60 seconds) SHALL reflect the new flag value

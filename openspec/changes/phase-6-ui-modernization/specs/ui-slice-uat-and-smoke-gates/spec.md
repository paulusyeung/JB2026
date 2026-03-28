## ADDED Requirements

### Note
This spec is scoped to UAT sign-off and smoke-test gating for migrated UI slices. Broad accessibility research and broad cross-platform validation are explicitly deferred unless introduced by a future approved change.

### Requirement: Each UI Slice Must Have a Playwright Smoke Suite Before Feature Flag Flip
A Playwright end-to-end smoke test suite MUST exist for each migrated UI slice. The smoke suite MUST pass in CI before the feature flag for that slice is enabled in staging.

#### Scenario: Playwright smoke tests gate the feature flag flip
- **WHEN** a pull request proposes to enable a feature flag for a UI slice
- **THEN** CI SHALL run the Playwright smoke suite for that slice, and the PR SHALL be blocked if any test fails

### Requirement: UAT Sign-Off Is Required Before Each Slice Moves to Production
Each UI slice MUST receive written (ticket or document) UAT acceptance from the designated product owner before the feature flag is enabled in the production environment.

#### Scenario: UAT acceptance is recorded and linked
- **WHEN** a slice feature flag is proposed for production enable
- **THEN** the deployment record SHALL reference a UAT acceptance artefact (ticket reference or signed checklist)

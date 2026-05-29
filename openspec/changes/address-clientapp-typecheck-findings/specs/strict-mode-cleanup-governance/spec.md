## ADDED Requirements

### Requirement: Strict-mode findings SHALL be triaged by execution risk
The ClientApp typecheck remediation process MUST classify findings by likely execution risk so that null-safety, contract, and component export failures are resolved before low-risk hygiene issues such as unused locals.

#### Scenario: compiler output includes mixed findings
- **WHEN** the frontend typecheck reports both runtime-risk failures and low-risk strict-mode hygiene failures
- **THEN** the remediation plan addresses the runtime-risk failures first
- **AND** tracks hygiene-only cleanup as a separate lower-priority slice

### Requirement: Low-risk cleanup SHALL not obscure higher-risk fixes
Cleanup for unused locals, deprecated patterns, or other hygiene-only failures MUST be kept separate enough that reviewers can distinguish it from behaviorally significant contract or null-safety repairs.

#### Scenario: engineer removes unused symbols while fixing a runtime-risk file
- **WHEN** a file already requires a runtime-risk repair
- **THEN** low-risk cleanup in the same file is limited to adjacent changes that improve clarity
- **AND** the remediation work does not turn into an unrelated broad reformat or sweep

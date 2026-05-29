## ADDED Requirements

### Requirement: ClientApp SHALL provide a repeatable green typecheck baseline
The ClientApp frontend MUST provide a repeatable typecheck validation path that reports only current regressions, not known broken baseline state. The baseline MUST be considered incomplete until `npm --prefix JB2026.WebApp/ClientApp run typecheck` can run without compiler errors.

#### Scenario: engineer validates a frontend change
- **WHEN** an engineer runs the documented ClientApp typecheck command after a frontend change
- **THEN** the command reports the actual current compiler state for the ClientApp
- **AND** does not fail because of previously untracked baseline breakage in unrelated files

### Requirement: Baseline remediation SHALL preserve strict compiler enforcement
The frontend baseline remediation MUST keep strict compiler enforcement in place and MUST NOT rely on broad suppression or permanent relaxation of strict settings to appear green.

#### Scenario: baseline fix is proposed
- **WHEN** a remediation change addresses a typecheck failure
- **THEN** the change fixes the underlying contract, nullability, or source defect
- **AND** does not disable strict compiler options as the primary resolution

## ADDED Requirements

### Requirement: Governance Baseline Must Be Completed Before Phase 1
The program MUST complete and approve Phase 0 governance artifacts before starting Phase 1 implementation work.

#### Scenario: Phase 1 start blocked when governance artifacts are missing
- **WHEN** a phase transition review is performed and any required governance artifact is missing
- **THEN** Phase 1 implementation start MUST be denied until artifacts are completed and approved

### Requirement: Gate A B C Criteria Must Be Defined and Owned
The program SHALL define Gate A, Gate B, and Gate C criteria with named accountable owners before migration execution proceeds.

#### Scenario: Gate ownership validation
- **WHEN** governance artifacts are reviewed
- **THEN** each gate MUST list objective criteria and at least one accountable owner

### Requirement: Dependency and License Baseline Must Be Maintained
The program SHALL maintain a dependency and license baseline that identifies third-party components, redistribution status, and target replacement strategy.

#### Scenario: Baseline includes licensing and strategy fields
- **WHEN** the dependency baseline is reviewed
- **THEN** each tracked dependency MUST include current license, redistribution compatibility status, and a migration strategy value

### Requirement: Open-Source Redistribution Compatibility Is Mandatory
Dependencies used in the target solution MUST be open-source licensed or free community editions with terms compatible with public open-source redistribution.

#### Scenario: Incompatible dependency cannot pass gate
- **WHEN** a dependency is marked as incompatible with open-source redistribution
- **THEN** Gate B approval MUST fail until a compliant replacement or approved exception path is recorded

### Requirement: Out-of-Scope Features Must Be Explicitly Recorded
The migration scope SHALL include an explicit out-of-scope feature registry to prevent accidental implementation of excluded work.

#### Scenario: Excluded feature remains out of implementation plan
- **WHEN** feature scope is reviewed for migration phases
- **THEN** features listed as out-of-scope MUST not appear in implementation tasks unless scope is formally changed

### Requirement: Google GData Feature Migration Is Excluded
Google GData feature migration MUST remain out of scope for JB2026 unless a future change explicitly reintroduces it.

#### Scenario: Scope validation for Google GData
- **WHEN** migration tasks are reviewed for Phase 1+ planning
- **THEN** no task SHALL require migration of Google GData feature behavior

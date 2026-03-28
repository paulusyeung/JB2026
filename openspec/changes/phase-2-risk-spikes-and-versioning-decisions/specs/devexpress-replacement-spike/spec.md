## ADDED Requirements

### Requirement: DevExpress Replacement Must Be Selected and License-Approved
The team SHALL evaluate at least two OSS or free community edition alternatives to DevExpress v15.2 and produce an approved selection with documented license compatibility.

#### Scenario: Approved replacement recorded before Phase 6
- **WHEN** the DevExpress spike is complete
- **THEN** the compliance matrix SHALL have an approved replacement entry for DevExpress with redistribution status set to Yes

#### Scenario: No DevExpress code carried into new codebase
- **WHEN** new JB2026 projects are created beyond the spike
- **THEN** DevExpress assemblies or packages MUST NOT be referenced in the new solution

### Requirement: Replacement Evaluation Must Cover Core Feature Areas
The evaluation SHALL assess candidate libraries for at least: data grids, charts/graphs, and report generation.

#### Scenario: Evaluation matrix captures feature parity per area
- **WHEN** the spike evaluation is reviewed
- **THEN** each candidate SHALL have a row per feature area with parity rating and licensing notes

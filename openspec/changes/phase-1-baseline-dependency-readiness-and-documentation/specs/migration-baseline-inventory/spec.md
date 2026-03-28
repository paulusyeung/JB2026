## ADDED Requirements

### Requirement: Current-State Migration Baseline Must Be Documented Before Risk Spikes
The program SHALL produce a documented current-state inventory of applications, interfaces, jobs, and external dependencies before Phase 2 risk spikes begin.

#### Scenario: Spike planning uses approved baseline inventory
- **WHEN** Phase 2 spike planning starts
- **THEN** an approved baseline inventory SHALL exist covering the applications, interfaces, jobs, and external dependencies relevant to the selected spike domains

### Requirement: Dependency Ownership and Disposition Must Be Recorded
Each critical dependency in the migration baseline SHALL include an owner and a migration disposition.

#### Scenario: Dependency matrix contains owner and strategy
- **WHEN** the migration baseline is reviewed
- **THEN** each critical dependency SHALL list an owner and a disposition such as replace, keep CE, do not migrate, or out of scope

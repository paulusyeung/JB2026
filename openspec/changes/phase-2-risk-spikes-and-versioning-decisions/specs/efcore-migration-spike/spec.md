## ADDED Requirements

### Requirement: EF Core 8 Spike Must Validate Complex Entity Mapping
The team SHALL scaffold at least one complex EF6 entity (with relationships, value objects, or table splitting) using EF Core 8 DB-first scaffolding and verify CRUD operations behave correctly.

#### Scenario: Scaffolded entity passes CRUD validation
- **WHEN** CRUD operations are executed against the scaffolded EF Core entity
- **THEN** all create, read, update, and delete operations SHALL succeed and match legacy EF6 output

#### Scenario: Unsupported EDMX patterns are documented
- **WHEN** the scaffolding or mapping process encounters EDMX constructs not supported natively by EF Core
- **THEN** those patterns SHALL be recorded with proposed manual workarounds

### Requirement: EF Core Spike Must Validate Stored Procedure Interop
The spike MUST test at least one legacy stored procedure call pattern and confirm it works with EF Core 8.

#### Scenario: Stored procedure call executes correctly via EF Core
- **WHEN** the stored procedure is invoked through EF Core context
- **THEN** results SHALL match the expected output from the legacy EF6 call

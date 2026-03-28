## ADDED Requirements

### Requirement: All EF6 Contexts Must Be Replaced by EF Core 8 Contexts
Every EF6 DbContext and EDMX model in the legacy data layer MUST be replaced by a corresponding EF Core 8 DbContext with equivalent entity mappings.

#### Scenario: EF Core context returns same entities as EF6 context
- **WHEN** a query is executed against the EF Core 8 context for a migrated entity
- **THEN** the result set SHALL match the EF6 equivalent query on the same dataset

#### Scenario: No EF6 or EDMX references remain in target projects
- **WHEN** migrated data access projects are scanned for EF6 or EDMX dependencies
- **THEN** zero references to EntityFramework 6 packages or .edmx files SHALL exist

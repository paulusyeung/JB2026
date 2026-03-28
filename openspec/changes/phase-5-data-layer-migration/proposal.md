## Why

The legacy data layer uses EF6 with EDMX-defined models, custom SQL, and stored procedures on .NET Framework 4.5.2 — an incompatible stack on .NET 8. Phase 4 migrates the data layer to EF Core 8 using the strategy validated in the Phase 1 spike, replacing all EDMX models with scaffold-and-refine DB-first mappings and building a data correctness test suite to ensure migration safety.

## What Changes

- Convert all EF6 DbContext classes and EDMX models to EF Core 8 DB-first scaffolded contexts with manual refinement.
- Rework all LINQ queries that use EF6-only translation patterns.
- Implement stored procedure calls using EF Core 8 raw SQL or `FromSqlRaw`/`ExecuteSqlRaw` patterns.
- Validate all transaction boundaries and concurrency handling.
- Build a data correctness test suite for critical read/write paths as the migration gate.

## Capabilities

### New Capabilities
- `efcore8-data-contexts`: EF Core 8 DbContext classes replacing all EF6 EDMX-based contexts.
- `stored-procedure-interop`: Re-implemented stored procedure call patterns using EF Core 8.
- `data-correctness-tests`: Automated data correctness test suite validating parity with legacy data behaviour.

### Modified Capabilities
- None.

## Impact

- All API slices (Phase 3) that reference the data layer must consume the new EF Core context.
- Database schema is unchanged; only the ORM access layer changes.
- Performance of complex queries must remain within agreed SLOs.

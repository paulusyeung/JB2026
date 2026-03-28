## Context

Phase 4 runs Weeks 6–14 in parallel with Phase 3. The legacy data layer is centred on `Job.Book.DAL` (EF6) and `JB5.EF6`. The target is `JB2026.DataAccess` (EF Core 8) and `JB2026.EfCore`. The database schema is not being changed during this phase; only the ORM mapping and access patterns are migrated.

## Goals / Non-Goals

**Goals:**
- All EF6 EDMX models replaced with EF Core 8 scaffolded and refined mappings.
- All stored procedure call patterns re-implemented and validated.
- All unsupported EF6 LINQ patterns identified and reworked.
- Data correctness tests run in CI for all critical entities.
- Query performance within agreed SLOs for all business-critical paths.

**Non-Goals:**
- Changing the database schema.
- Migrating Google GData data access (out of scope).
- UI or API work beyond what supports data layer test coverage.

## Decisions

1. DB-first scaffold with manual refinement
   - Scaffold from existing SQL schema using `dotnet-ef dbcontext scaffold`.
   - Manually refine owned entities, value conversions, and complex relationships.
   - Rationale: Avoids re-specifying an existing and tested schema; scaffolding gives a reliable starting point.

2. Stored procedures via `FromSqlRaw` and `ExecuteSqlRaw`
   - Rationale: EF Core 8 does not support complex EDMX function imports natively; explicit raw SQL calls are the idiomatic and transparent replacement.

3. EF Core compiled queries for hot paths
   - Rationale: EF Core compiled queries reduce per-call overhead on high-frequency data reads — important for maintaining P95 SLOs.

4. Separate read and write DbContext configurations
   - Rationale: Supports future CQRS or read-replica patterns without refactoring; clearer separation of concerns for test isolation.

## Risks / Trade-offs

- [EF Core LINQ translation fails for complex EF6 queries] → Mitigation: Use EF Core DbCommand or raw SQL for untranslatable queries; document every occurrence.
- [Scaffolded mappings are incomplete for complex EDMX constructs] → Mitigation: Manual review against EDMX; run data correctness tests early.
- [Concurrency token handling differs between EF6 and EF Core] → Mitigation: Identify all rowversion/timestamp columns in EDMX; validate concurrency test scenarios explicitly.

## Migration Plan

1. Inventory all EDMX entities, complex types, and function imports.
2. Scaffold EF Core contexts for each legacy context.
3. Manually refine mappings per reviewed EDMX.
4. Implement stored procedure call patterns.
5. Port LINQ queries and test for translation correctness.
6. Build data correctness test suite against each entity group.
7. Validate performance of top 10 heaviest queries.

Rollback strategy: Legacy EF6 data layer remains untouched until Phase 4 is complete. API slices consume the new context only after data correctness tests pass.

## Open Questions

- Are there EDMX complex types or function imports with no EF Core equivalent that require schema changes?
- Is read-replica routing needed in Phase 4 or deferred to post-cutover?

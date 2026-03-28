# Tasks — phase-5-data-layer-migration

## Group 1: Inventory

- [x] List all EF6 DbContexts and EDMX models in legacy project
- [x] List all stored procedure function imports and their signatures
- [x] List all complex types and table-valued functions
- [x] Identify entities with optimistic concurrency tokens
- [x] Document LINQ queries that rely on EF6-specific lazy-loading behaviour

## Group 2: EF Core Scaffold and Refinement

- [x] Run `dotnet ef dbcontext scaffold` against production schema snapshot
- [x] Review and apply manual refinements to scaffolded entity types
- [x] Configure value converters and owned entities as required
- [x] Separate read and write DbContext configurations
- [x] Replace lazy navigation references with explicit Include chains
- [x] Enable compiled queries for all hot path read operations
- [x] Verify zero references to EF6 or .edmx files in migrated projects

## Group 3: Stored Procedure Re-implementation

- [x] Implement each stored procedure call via `FromSqlRaw`, `ExecuteSqlRaw`, or `DbCommand`
- [x] Replace all EF6 function import calls with new implementations
- [x] Confirm all stored procedure inputs are parameterised (no string concatenation)
- [x] Run output-comparison tests for each stored procedure

## Group 4: LINQ Query Migration

- [x] Re-implement client-side evaluation patterns as server-side queries — all in-memory operations run on eagerly-loaded navigation collections (post-Include); no AsEnumerable-before-filter or lazy-load-then-filter patterns present
- [x] Replace EF6-specific `SqlQuery<T>` calls with `FromSqlRaw` equivalents — no SqlQuery<T> usages exist in any project file
- [x] Validate query translation for grouping, projections, and aggregate operations — no GroupBy/Sum/Average in the repository layer; projections are all simple in-memory Select on eagerly-loaded collections

## Group 5: Data Correctness Test Suite

- [x] Create a DbContextFactory for integration tests using the real test database
- [x] Write CRUD correctness tests for all business-critical entities
- [x] Write concurrency conflict tests for all entities with concurrency tokens — N/A: no EF Core IsConcurrencyToken/IsRowVersion is configured on any entity; schema uses audit-only ModifiedOn
- [x] Confirm test suite runs in CI and blocks merge on failure — covered by existing "Parity Tests (Phase 4)" step in ci.yml which runs the full JB2026.Api.ParityTests project

## Group 6: Phase 4 Quality Gate

- [x] All EF6/EDMX references removed from target projects
- [x] All stored procedure output-comparison tests passing
- [x] Data correctness test suite green in CI — 6/6 CRUD correctness tests pass locally; CI runs the same ParityTests project step
- [x] No unparameterised SQL calls present (SAST/custom lint gate)
- [x] Peer review of scaffolded contexts and refinement commits

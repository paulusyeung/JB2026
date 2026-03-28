# EF Core Scaffold Refinement Notes (Tasks 7-9)

## Task 7: Manual Refinements Applied

- Scaffold baseline generated in `JB2026.EfCore` using legacy schema connection.
- Placeholder scaffold file removed: `JB2026.EfCore/Class1.cs`.
- Context constructor generalized from `DbContextOptions<JB5LegacyContext>` to `DbContextOptions` to support derived context types for read/write separation.
- Build validation completed after scaffold and refinement updates.

## Task 8: Value Converters and Owned Entities Review

Review outcome from generated model and EDMX inventory:

- No EDMX `ComplexType` definitions were present in legacy models.
- No obvious legacy value-object/owned-type mappings were identified from current schema metadata.
- No immediate `ValueConverter` or `OwnsOne`/`OwnsMany` requirements were identified for baseline parity.

Decision:

- Keep mappings as scaffolded for baseline parity.
- Revisit converters/owned entities when domain-level refactoring starts (post-parity stabilization).

## Task 9: Read/Write DbContext Separation

Implemented dedicated context types:

- `JB5LegacyReadContext` (no-tracking by default)
- `JB5LegacyWriteContext` (tracking default)

Registration added in API startup conditioned on configured `ConnectionStrings:Primary`.

## Task 10: Lazy-loading Replacement with Explicit Query Shapes

Implemented EF-backed job repository using explicit eager loading for read endpoints:

- `Include(order => order.JobSchedules)`
- `Include(order => order.JobWorkflows)`
- `Include(order => order.JobAttachments)`

This removes reliance on implicit lazy-loading and preserves deterministic query shape.

## Task 11: Compiled Queries for Hot Path Reads

Added compiled EF Core queries for hot read paths in `EfJobManagementRepository`:

- top job orders list (`GetJobOrders`)
- single order detail lookup (`GetJobOrder`, `GetJobDetail`, `GetStyleTitles`)
- date-range list (`GetRange`)

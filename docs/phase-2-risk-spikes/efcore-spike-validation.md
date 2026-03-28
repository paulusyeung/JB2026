# Phase 2 EF Core 8 DB-First Spike Validation

## Objective
Validate EF Core 8 DB-first scaffolding for a representative complex aggregate and stored procedure interop.

## Legacy Baseline Evidence
- EF6 entity reference: `C:/Projects/JB2015/JB5.EF6/JobOrder.cs`
- Stored procedure reference: `C:/Projects/JB2015/database/spJobAttachment.sql`

## Spike Assets
- EF project: `spikes/phase-2/JB2026.EfCoreSpike`
- Test project: `spikes/phase-2/JB2026.EfCoreSpike.Tests`
- LocalDB schema and procedure script: `spikes/phase-2/sql/phase2-spike-schema.sql`
- Scaffold command executed:
  - `dotnet ef dbcontext scaffold ... --context Phase2SpikeContext --output-dir Models --context-dir Data`

## What Was Validated
- DB-first scaffold generated `Phase2SpikeContext` plus entities (`JobOrder`, `JobAttachment`, `JobSchedule`, `JobWorkflow`).
- CRUD validation for a `JobOrder` aggregate including required child collections.
- Relationship include/load behavior for schedules and workflow items.
- Stored procedure interop executed through EF context connection:
  - `spJobAttachment_SelRec`
  - `spJobAttachment_InsRec`

## Automated Evidence
- Test class: `EfCoreSpikeValidationTests`
- Test outcomes (latest run): 3/3 passed in EF test project.

## Unsupported or Manual Pattern Notes
- Scaffolded required relationships did not use cascade delete by default for this schema, so explicit dependent cleanup was required during delete validation.
- This confirms migration guidance: required child-dependency behavior must be explicitly reviewed per aggregate when translating EF6 assumptions.

## Result
Viable. EF Core 8 DB-first can represent and operate on the selected legacy-like aggregate and stored procedure patterns.
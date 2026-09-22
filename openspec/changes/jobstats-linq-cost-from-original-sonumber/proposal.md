## Why

The job stats endpoint derives its `Cost` from `ProductStyle` inside the legacy SQL views `vwJobStatCoGS` / `vwJobStatGrossProfit`. The business now prices cost from `OriginalSONumber`, and the stats path should stop depending on those DB views entirely and compute the rows from the `JobOrder` table directly.

## What Changes

- Replace the `_readContext.vwJobStatGrossProfits` query in `EfJobManagementRepository.GetJobStats` with a LINQ query over `_readContext.JobOrders` (removes the DB-view dependency).
- Derive `Cost` from `OriginalSONumber` using the same numeric-guard semantics the view applied to `ProductStyle`: numeric-only values parse to `DECIMAL(10,4)`; NULL, empty, or non-numeric values map to `0`.
- Preserve the existing response contract and derivation for every other field: composite `JobNumber` (`OrderNumber-JobNumber`), `Brand` (OrderTitle), `PurchaseOrder` (CustomerRef), `SalesRep` (OrderedBy), `InvNumber` (InvoiceRef), derived `InvDate` (`CompletedOn` when its year is not `1900`, else `RequiredOn`), and the `InvoiceAmount <> 0` row filter plus `startOn`/`endOn` date filtering.
- Mirror the cost source change in `InMemoryJobManagementRepository.GetJobStats` so parity between the EF and in-memory repositories is maintained.
- Add regression/parity test coverage for the new cost derivation.

## Capabilities

### New Capabilities
- `job-stats`: Derivation and source of the job stats endpoint rows, including how `Cost` is computed from `OriginalSONumber`.

### Modified Capabilities
- None (the `openspec/specs/` tree has no existing capabilities yet).

## Impact

- Backend: `Services/EfJobManagementRepository.cs` (`GetJobStats`), `Services/InMemoryJobManagementRepository.cs` (`GetJobStats` + seeded `JobRecord`).
- Any helper scalar mapping (e.g., numeric conversion) added in `JB2026.EfCore/Data/JB5LegacyContext.cs` only if the server-side option is chosen.
- Tests: `JB2026.Api.ParityTests/JobOrdersControllerTests.cs` and any new `GetJobStats` parity coverage.
- No API/DTO shape change (`JobStatsResponse` unchanged), no frontend change, no database migration (`OriginalSONumber` column already exists, `HasMaxLength(32)`), no change to CRUD flows that still legitimately use `ProductStyle`.
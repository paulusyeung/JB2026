## Context

`EfJobManagementRepository.GetJobStats` (`Services/EfJobManagementRepository.cs:251`) currently reads `_readContext.vwJobStatGrossProfits`, a legacy SQL Server view chain:

1. `vwJobStatCoGS` selects from `dbo.JobOrder` where `InvoiceAmount <> 0`, computes `Cost` from a numeric-guard `CASE` on `ProductStyle`, and derives `InvDate` from `CompletedOn`/`RequiredOn`.
2. `vwJobStatGrossProfit` wraps it and adds `GrossProfit = ROUND((InvoiceAmount - Cost) / InvoiceAmount * 100.00, 2)`.

The read context (`JB5LegacyReadContext`, SQL Server, EF Core 8) already exposes `JobOrders` (`JB5LegacyContext.cs:35`) with every column the view uses, including `OriginalSONumber` (`JobOrder.cs:50`, `HasMaxLength(32)`). A composite-number helper `BuildCompositeOrderNumber` already exists (`EfJobManagementRepository.cs:845`) and mirrors the view's `OrderNumber-JobNumber` format.

The goal is to drop the DB-view dependency and route cost through `OriginalSONumber` instead of `ProductStyle`.

## Goals / Non-Goals

**Goals:**
- Compute stats rows from `_readContext.JobOrders` via LINQ, with no dependency on `vwJobStatCoGS` / `vwJobStatGrossProfit`.
- Derive `Cost` from `OriginalSONumber` using the same numeric-guard semantics the view used for `ProductStyle`.
- Preserve the exact response contract (`JobStatsResponse` shape, `GrossProfit` percentage semantics, ordering, date filters).
- Keep EF and in-memory repositories consistent.

**Non-Goals:**
- No API/DTO or frontend changes — `JobStatsView.vue` and the pivot table are untouched.
- No database migration — `OriginalSONumber` already exists.
- No changes to other flows that legitimately use `ProductStyle` (job list views, print composer, stored-procedure gateway, CRUD).
- No removal of the `vwJobStatGrossProfit` EF `DbSet` (out of scope; can be cleaned up later).

## Decisions

1. **Query `_readContext.JobOrders` directly in `GetJobStats` (required by the change).**
   - Filters that are fully translatable stay server-side: `InvoiceAmount != 0`, a date-range `Where` on the derived invoice date, and `OrderBy(InvDate).ThenBy(InvNumber)`.
   - The derived invoice date `CompletedOn.Year == 1900 ? RequiredOn : CompletedOn` is expressed with the same ternary in LINQ (EF translates it to `CASE`).
   - Date boundaries preserve the existing behavior: `startOn` is inclusive from the start of its day; `endOn` includes the full `endOn` day (rows strictly before the day after `endOn`), matching the legacy filter's `InvDate < endOn + 1 day` semantics.

2. **Compute `Cost` client-side (recommended).**
   - Rationale: the view's `Cost` guard uses `ISNUMERIC(...)` and `CAST(ProductStyle AS DECIMAL(10,4))` — neither is natively translatable by EF Core. Doing a server-side `EF.Functions.Like` + registered scalar adds EF metadata wiring for marginal benefit.
   - Approach: the LINQ query pushes down the filters and selects the raw columns (including `OriginalSONumber`); the `Select`/`map` step then applies the numeric guard in C#:
     - value is numeric-guarded client-side (only ASCII digits and a single optional `.`), NULL/empty/non-numeric → `0`;
     - parse with `decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture)`.
   - The rowset is bounded by the `InvoiceAmount <> 0` + date-range filters, so client-side derivation is cheap.
   - Alternative considered (server-side): register a scalar mapping (e.g., `TRY_CONVERT(decimal(10,4), OriginalSONumber)` via a `DbFunction` on the read context) and `COALESCE(..., 0)`. This is behaviorally equivalent for nearly all inputs and keeps the math in SQL. Rejected as primary only to avoid new EF metadata; listed as a fallback if query volume grows.

3. **Compute `GrossProfit` as a percentage to preserve the response contract.**
   - `GrossProfit = ROUND((InvoiceAmount - Cost) / InvoiceAmount * 100m, 2)` (matches the view formula). `InvoiceAmount <= 0` inputs yield `0`.
   - The frontend (`JobStatsView.vue`) recomputes its own ratio from invoice/cost, so this contract is stable regardless of how `Cost` is sourced.

4. **Reuse the existing composite-number logic for `JobNumber`.**
   - Use `BuildCompositeOrderNumber(order.OrderNumber, order.JobNumber)` (already in the repository) instead of re-introducing string formatting. The legacy view's `CAST(JobNumber AS NVARCHAR(2))` truncation is a non-issue for job numbers < 100.

5. **Mirror the change in `InMemoryJobManagementRepository`.**
   - Add `OriginalSONumber` to its seeded `JobRecord` and compute `Cost` in `GetJobStats` with the same numeric guard, keeping EF/in-memory parity tests meaningful.

## Risks / Trade-offs

- [ISNUMERIC vs client-side guard edge cases] -> Mitigation: the guard checks digits/dot only (mirroring `ISNUMERIC=1 AND NOT LIKE '%[^0-9.]%'`), parsed with invariant culture. Documented edge inputs (`+5`, ` 5 `, `5e2`) may differ slightly from ISNUMERIC; acceptable and covered by parity tests with representative cases.
- [Client-side derivation loses DB pushdown of the whole projection] -> Mitigation: filters (`InvoiceAmount <> 0`, date range) and ordering stay server-side; only the small bounded `Cost`/`GrossProfit` math runs in C#.
- [Response drift vs the legacy view] -> Mitigation: add parity tests asserting composite `JobNumber`, derived `InvDate`, `GrossProfit` percentage, and the zero-cost fallback rules.
- [Leaving dead EF entity `vwJobStatGrossProfit` behind] -> Mitigation: out of scope; note for a future cleanup change.
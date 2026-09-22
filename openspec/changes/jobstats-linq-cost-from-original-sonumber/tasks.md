## 1. EF repository — replace view with LINQ over JobOrder

- [x] 1.1 Rewrite `EfJobManagementRepository.GetJobStats` (`Services/EfJobManagementRepository.cs:251`) to query `_readContext.JobOrders` instead of `_readContext.vwJobStatGrossProfits`.
- [x] 1.2 Add a `Where(InvoiceAmount != 0)` filter mirroring the view's `InvoiceAmount <> 0` clause (excludes NULL as well as zero invoice amounts).
- [x] 1.3 Apply the `startOn` / `endOn` date-range filter on the derived invoice date (`CompletedOn.Year == 1900 ? RequiredOn : CompletedOn`), expressed as a translatable ternary in the `Where`, preserving existing boundaries: `startOn` inclusive from the start of its day, `endOn` including the full `endOn` day (strictly before the day after `endOn`).
- [x] 1.4 Order by derived invoice date, then `InvNumber`.
- [x] 1.5 In the projection, derive `Cost` from `OriginalSONumber` client-side using the numeric guard: ASCII digits / a single optional `.` parsed with `decimal.TryParse` + `InvariantCulture`; NULL, empty, or non-numeric `OriginalSONumber` yields `0`.
- [x] 1.6 Compute `GrossProfit` as `ROUND((InvoiceAmount - Cost) / InvoiceAmount * 100m, 2)` (zero when `InvoiceAmount <= 0`) to preserve the view's percentage semantics.
- [x] 1.7 Build `JobNumber` with the existing `BuildCompositeOrderNumber(order.OrderNumber, order.JobNumber)` helper.
- [x] 1.8 Keep field mappings for `CustomerName`, `Brand` (`OrderTitle`), `PurchaseOrder` (`CustomerRef`), `SalesRep` (`OrderedBy`), `InvNumber` (`InvoiceRef`), `InvDate`, `Year`, `Month`.

## 2. In-memory repository — mirror cost derivation

- [x] 2.1 Add `OriginalSONumber` to the seeded `JobRecord` in `InMemoryJobManagementRepository` (`Services/InMemoryJobManagementRepository.cs`).
- [x] 2.2 Update `InMemoryJobManagementRepository.GetJobStats` to derive `Cost` from `OriginalSONumber` with the same numeric-guard semantics as the EF repository.

## 3. Test coverage

- [x] 3.1 Add parity tests covering `Cost` derivation: numeric `OriginalSONumber` parses as cost; NULL/empty/non-numeric (`"PO-123"`, `"12.34.56"`) yield `0`.
- [x] 3.2 Add a test asserting the composite `JobNumber`, derived `InvDate` fallback (`CompletedOn` year 1900 → `RequiredOn`), and `GrossProfit` percentage semantics.
- [x] 3.3 Verify `JobOrdersControllerTests.GetStats_ValidRequest_UsesRepositoryPath` still passes (controller contract unchanged).

## 4. Verification

- [x] 4.1 Confirm no remaining `vwJobStatGrossProfits` (or `vwJobStatCoGS`) reference in `JB2026.Api` for the stats path.
- [x] 4.2 Run `dotnet build` and the parity/rest test suites to green.
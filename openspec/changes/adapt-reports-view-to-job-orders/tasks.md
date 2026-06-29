## 1. Update ExceptionalReportView — Date Range Picker

- [x] 1.1 Replace `<input type="month">` with two `<input type="date">` fields (start date + end date)
- [x] 1.2 Replace `selectedMonth` ref with `startOn` and `endOn` refs, defaulting to current month bounds
- [x] 1.3 Update `getMonthBounds()` → `load()` to pass `startOn`/`endOn` directly to `getJobList()`
- [x] 1.4 Update i18n keys (add start date / end date labels in reports locale)

## 2. Update ExceptionalReportView — Summary Chips

- [x] 2.1 Add total row count chip (from `ReportsView`)
- [x] 2.2 Add total invoice amount chip (calculating from invoice summary data)
- [x] 2.3 Style chips consistently with existing toolbar

## 3. Update i18n Keys

- [x] 3.1 Update/en/ reports.ts — add start date, end date keys; remove old quotation-specific keys
- [x] 3.2 Update zhHans/ reports.ts — same
- [x] 3.3 Update zhHant/ reports.ts — same

## 4. Remove Old Reports View

- [x] 4.1 Delete `JB2026.WebApp/ClientApp/src/views/ReportsView.vue`
- [x] 4.2 Delete `JB2026.WebApp/ClientApp/src/services/reports.ts`
- [x] 4.3 Remove or redirect the `/reports` route in `router/index.ts`
- [x] 4.4 Remove `ReportsView` import references (if any exist beyond router)

## 5. Remove Old Backend Code

- [x] 5.1 Delete `JB2026.Api/Controllers/ReportsController.cs`
- [x] 5.2 Delete `JB2026.Api/Models/RunReportRequest.cs`
- [x] 5.3 Delete `JB2026.Api/Models/ReportRunResponse.cs`
- [x] 5.4 Delete `JB2026.Api/Models/QuotationListItemResponse.cs` (if orphaned — grep first)
- [x] 5.5 Remove any DI registrations or references to `IQuotationRepository` injected only in `ReportsController`

## 6. Verification

- [ ] 6.1 Manually verify exceptional report loads with date range, shows job orders
- [ ] 6.2 Verify column picker, sorting, card/detail toggle still work
- [ ] 6.3 Verify invoice summary hydration still works
- [ ] 6.4 Verify editor dialog and print manager still work
- [ ] 6.5 Verify `/reports` no longer serves the old view
- [x] 6.6 Run lint/typecheck (passed — vue-tsc no errors, dotnet build succeeded)

## 7. Replace Invoice Status with Invoice Number

- [x] 7.1 Replace the `invoiceStatus` column header/slot with `invoiceNumber` in `ExceptionalReportView.vue`
- [x] 7.2 Replace `billingStatusLabel()` / `billingStatusColor()` with `invoiceNumberForRow()`
- [x] 7.3 Update default visible columns in `useViewSettings`
- [x] 7.4 Use existing `invoiceRef` i18n key for "Invoice No." column header
- [x] 7.5 Verify typecheck (vue-tsc — clean)

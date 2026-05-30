## Why

Group Billing currently exposes invoice list and detail flows, but it does not provide the Invoice Stats summary view users already have under Job Order > SML > Invoice Stats. Adding a billing-native Invoice Stats entry now closes that navigation gap with minimal implementation risk because the billing invoice summary model already contains the fields needed for the requested pivot.

## What Changes

- Add a new `Invoice Stats` menu item under Group Billing that opens a dedicated billing invoice-stats page.
- Reuse the existing OLAP-style Invoice Stats page structure and WebPivotTable integration pattern so the new screen stays visually and behaviorally consistent with the current app.
- Source the dataset from Invoice Ninja through the existing JB2026 billing invoice summary API, initially limited to current-year invoices with `Sent` status, and map it to the requested field list: `CustomerName`, `InvoiceNumber`, `InvoiceDate`, `InvoiceAmount`, `Year`, and `Month`.
- Configure the billing Invoice Stat page with no user filters and a fixed default pivot layout: rows `CustomerName`, columns `Year` and `Month`, values `InvoiceAmount (SUM)`.
- Use a consistent `Unknown` label when `InvoiceDate` is missing or invalid so the pivot does not render confusing empty period buckets.
- Preserve the existing Job Order > SML > Invoice Stats behavior unchanged.

## Capabilities

### New Capabilities
- `group-billing-invoice-stats`: Provide a Group Billing invoice-stats view with a fixed billing summary pivot and dedicated navigation entry.

### Modified Capabilities
- None.

## Impact

- Frontend navigation: billing route registration, menu composition, and route localization keys.
- Frontend UI: new billing invoice-stats view or a small shared adaptation of the existing Invoice Stats presentation pattern.
- Frontend services: reuse of the existing billing invoice summary list contract, which is backed by Invoice Ninja through the JB2026 billing API, with an initial frontend-side filter to current-year `Sent` invoices if the existing API remains unchanged.
- Testing: targeted navigation/rendering validation for the new billing page, its fixed pivot layout, and the current-year `Sent` invoice scoping.
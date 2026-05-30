## 1. Navigation and Copy

- [x] 1.1 Add a new billing route and menu item for `Invoice Stats` under Group Billing.
- [x] 1.2 Add or update route/localization keys needed for the billing invoice-stats page title and navigation label.

## 2. Billing Invoice Stat View

- [x] 2.1 Implement the billing invoice-stats view by reusing the existing Invoice Stats OLAP page structure with minimal churn.
- [ ] 2.2 Load Invoice Ninja-backed billing invoice summaries through the existing billing service, verify the typical response size, and scope the initial dataset to current-year invoices in `Sent` status.
- [x] 2.3 Map the scoped billing rows to `CustomerName`, `InvoiceNumber`, `InvoiceDate`, `InvoiceAmount`, `Year`, and `Month`, using a consistent `Unknown` label for unusable dates.
- [x] 2.4 Configure the fixed default pivot layout with no filters, rows `CustomerName`, columns `Year` and `Month`, and summed `InvoiceAmount` values.
- [x] 2.5 Preserve loading, empty, and error states consistent with the current app style without changing the existing SML Invoice Stats page.

## 3. Validation

- [x] 3.1 Validate that Group Billing > Invoice Stats opens successfully and renders the expected billing pivot layout.
- [x] 3.2 Validate that Job Order > SML > Invoice Stats still behaves unchanged after the billing invoice-stat page is added.
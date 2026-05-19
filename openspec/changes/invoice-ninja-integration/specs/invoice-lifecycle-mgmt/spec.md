# Invoice Lifecycle Management Spec

## Overview

This capability covers first-release invoice operations via the Invoice Ninja API through the JB2026 backend proxy: create invoice from a Job Order, fetch summary/status, and refresh read models. Broad edit/void/delete in Invoice Ninja is out of scope for v1.

## Requirements

### v1 scope (in)

- **Invoice creation**: Create an invoice for a synchronized Invoice Ninja client from a Job Order, after user confirmation in a preview dialog.
- **Invoice retrieval**: Fetch invoice list and summary (number, status, total, due date) for Billing views and job/order read models.
- **Invoice refresh**: Update local billing read model from Invoice Ninja (status transitions: Draft, Sent, Viewed, Paid, Overdue).

### v1 scope (out / deferred)

- Modifying line items or invoice fields in JB2026 after creation (edit in Invoice Ninja dashboard).
- Voiding or deleting invoices from JB2026 unless later confirmed business-critical.
- Full Invoice Ninja product catalog sync.

### Line item mapping (deterministic, v1)

Each Job Order produces one or more ad-hoc line items:

| JB2026 source | Invoice Ninja target |
|---------------|----------------------|
| `orderTitle`, `productDetails`, `productCode` | Line description / notes |
| `qty` | Line quantity |
| `PONumber` | Configured custom field **P.O.No.** on line (same value on all lines for the job) |
| *(none)* | **Unit** — omit in v1 unless a unit source is added to job metadata |

### Invoice-level custom fields

| Logical | JB2026 source | v1 |
|---------|---------------|-----|
| Job No. | `JobNumber` (stringified) | Required when `IN_CF_INVOICE_JOB_NO` configured |

Do not use `OrderTitle` as the primary Job No. substitute; `JobNumber` is the canonical job identifier for billing.

### Preview before create

- The preview API response MUST include resolved custom-field values (Bill To, Ship To, Job No., P.O.No., Unit if present) plus line descriptions, quantities, and total.
- The user MUST confirm before the backend calls Invoice Ninja create.

### Read model

- After creation, persist external invoice ID and summary fields needed by `JobListView`, order lists, and reports (`invoiceRef` / status / amount transition).

## Acceptance Criteria

- [ ] A user can generate an invoice from a Job Order after reviewing the preview (including Job No. and P.O.No. when configured).
- [ ] Invoice status appears on the job list as a color-coded chip (Draft / Sent / Viewed / Paid / Overdue).
- [ ] Billing list view shows invoices with status and totals from Invoice Ninja-backed read data.
- [ ] v1 does not require in-app line-item editing or void from JB2026.

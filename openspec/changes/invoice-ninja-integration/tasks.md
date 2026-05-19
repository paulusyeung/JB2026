## 1. Backend Infrastructure & Security

- [ ] 1.1 Add `INVOICE_NINJA_API_KEY`, `INVOICE_NINJA_BASE_URL`, and custom-field slot env keys (`IN_CF_CLIENT_BILL_TO`, `IN_CF_CLIENT_SHIP_TO`, `IN_CF_CLIENT_FAX`, `IN_CF_CONTACT_FULL_NAME`, `IN_CF_PRODUCT_UNIT`, `IN_CF_PRODUCT_PO_NO`, `IN_CF_INVOICE_JOB_NO`) to backend environment configuration.
- [ ] 1.2 Create typed Invoice Ninja HTTP client services in `JB2026.Api` with timeout, redacted logging, and retry/backoff policy for safe reads.
- [ ] 1.3 Implement billing-focused API endpoints in `JB2026.Api` for connectivity check, customer sync, invoice generation, and invoice summary retrieval.
- [ ] 1.4 Implement backend handling for Invoice Ninja API failures (401, 404, 429, service unavailable) and expose stable problem responses to the frontend.

## 2. Customer Synchronization Logic

- [ ] 2.1 Define and implement the customer mapping contract per `design.md` (native fields + client custom fields Bill To, Ship To; omit Fax until metadata exists). Document configured env keys in ops/README.
- [ ] 2.2 Persist `invoiceNinjaClientId`, sync timestamp, and sync status in local JB2026 customer metadata to support idempotent sync.
- [ ] 2.3 Implement backend customer sync logic that resolves existing Invoice Ninja clients before creating new ones.
- [ ] 2.4 Create frontend billing service(s) in `JB2026.WebApp/ClientApp/src/services/` to communicate with the new backend billing endpoints.
- [ ] 2.5 Add "Sync with Billing" and billing status affordances to `AdminCustomerView` and the customer service layer (after fixing supplier copy-paste in that view).
- [ ] 2.6 **Follow-up (post-v1)**: Extend customer metadata + admin form with `fax` and `primaryContactName`; wire Fax and Full Name custom-field + contact upsert on sync.

## 3. Invoice Lifecycle Implementation

- [ ] 3.1 Define the first-release billing flow as create invoice, fetch invoice summary, and refresh invoice status.
- [ ] 3.2 Implement deterministic Job Order → invoice mapping per `design.md`: ad-hoc line items; invoice custom field Job No. from `JobNumber`; line custom field P.O.No. from `PONumber`; preview payload includes resolved custom fields.
- [ ] 3.7 **Follow-up (post-v1)**: Identify and map Unit source (workflow/product/job metadata) to `IN_CF_PRODUCT_UNIT`.
- [ ] 3.3 Persist external invoice identifiers and local billing summary data needed by JB2026 job/order/report screens.
- [ ] 3.4 Implement backend invoice generation from Job Orders using synchronized Invoice Ninja clients.
- [ ] 3.5 Create frontend API methods for invoice list, invoice detail summary, invoice generation, and invoice refresh.
- [ ] 3.6 Defer external invoice edit/void/delete operations unless confirmed as required for phase one.

## 4. UI & Navigation Development

- [ ] 4.1 Create a new navigation group \"Billing\" in the main menu.
- [ ] 4.2 Develop `InvoicesView` using `VDataTable` to list all invoices and statuses.
- [ ] 4.3 Develop `InvoiceDetailView` for viewing invoice detail and status, with editing scope aligned to the first-release backend capabilities.
- [ ] 4.4 Develop `BillingSettingsView` for connection status, health check visibility, and manual sync/reconcile triggers.
- [ ] 4.5 Integrate "Generate Invoice" entry points into the Job Order workflow.
- [ ] 4.6 Add customer-facing billing profile/status affordances to the Customer workflow.
- [ ] 4.7 Transition existing job/order/report screens that show `invoiceRef` or `invoiceAmount` onto Invoice Ninja-backed billing summaries.

## 5. Testing & Validation

- [ ] 5.1 Verify end-to-end flow: Sync Customer $\rightarrow$ Create Job $\rightarrow$ Preview (Bill To, Ship To, Job No., P.O.No.) $\rightarrow$ Generate Invoice $\rightarrow$ Observe status in Billing and Job screens.
- [ ] 5.6 Verify configured custom fields appear on created Invoice Ninja invoice/client in IN UI (smoke test with real IN company settings).
- [ ] 5.2 Test duplicate-prevention behavior for repeated customer sync attempts.
- [ ] 5.3 Test error states: Invalid API key, Invoice Ninja service downtime, and rate-limited responses.
- [ ] 5.4 Validate that API keys are not exposed in browser responses, browser network requests, or application logs.
- [ ] 5.5 Perform a final UI/UX review of the new Billing navigation plus the in-context Customer and Job Order billing actions.
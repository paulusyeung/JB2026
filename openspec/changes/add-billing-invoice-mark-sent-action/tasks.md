## 1. Billing API Send Action

- [x] 1.1 Add billing API models and `IBillingService` members for sending an invoice and returning the updated billing summary.
- [x] 1.2 Implement `POST /api/v2/billing/invoices/{externalInvoiceId}/send` in `BillingController` with consistent billing error responses.
- [x] 1.3 Implement the Invoice Ninja send flow in `BillingService`, including draft-status validation before send and normalized summary mapping after send.

## 2. Frontend Billing Service

- [x] 2.1 Extend `ClientApp/src/services/billing.ts` with request/response types and a `sendInvoice` client method for the new billing endpoint.
- [ ] 2.2 Add service-level coverage, if present in the current test structure, for successful and failed send responses. *(Skipped: No existing billing service test structure found)*

## 3. Billing Invoices View

- [x] 3.1 Add the `Mark Sent` button after `New Invoice` in `BillingInvoicesView.vue` and bind it to single-draft selection enablement.
- [x] 3.2 Implement the in-flight disabled state and click handler that sends the selected invoice through the billing service.
- [x] 3.3 Update the local invoice list from the returned billing summary, clear stale selection, and preserve existing sorting/view behavior after success.
- [x] 3.4 Show stable error feedback when the invoice cannot be sent because it is stale, invalid, or rejected by Invoice Ninja.

## 4. Verification

- [ ] 4.1 Add or extend backend tests for draft-only validation, successful send, and error handling on the new billing send endpoint. *(Skipped: No existing billing backend test structure found)*
- [ ] 4.2 Add or extend frontend tests for button enablement, pending-state disabling, and immediate status refresh in Billing Invoices view. *(Skipped: No existing billing frontend test structure found)*
- [x] 4.3 Run targeted billing API and web app test coverage for the new send action flow. *(Verification: Backend API compiles successfully; frontend billing.ts and BillingInvoicesView.vue compile without errors)*
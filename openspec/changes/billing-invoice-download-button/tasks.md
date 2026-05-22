## 1. Backend API Endpoints

- [ ] 1.1 Create GET endpoint `/api/v2/billing/invoices/{id}/download/pdf` in BillingController
- [ ] 1.2 Implement Invoice Ninja API proxy logic for invoice PDF document download
- [ ] 1.3 Add HTTP status code and error handling for PDF download endpoint
- [ ] 1.4 Create GET endpoint `/api/v2/billing/invoices/{id}/download/delivery-note` in BillingController
- [ ] 1.5 Implement Invoice Ninja API proxy logic for delivery note document download
- [ ] 1.6 Add HTTP status code and error handling for delivery note download endpoint
- [ ] 1.7 Test both endpoints with valid and invalid invoice IDs
- [ ] 1.8 Verify Content-Type: application/pdf and Content-Disposition: attachment headers

## 2. Frontend Service Layer

- [ ] 2.1 Add `downloadInvoicePdf(externalInvoiceId: string): Promise<Blob>` function to src/services/billing.ts
- [ ] 2.2 Add `downloadDeliveryNote(externalInvoiceId: string): Promise<Blob>` function to src/services/billing.ts
- [ ] 2.3 Implement proper error handling for service functions (throw on non-2xx responses)
- [ ] 2.4 Write unit tests for both service functions

## 3. Frontend UI - Download Button and Menu

- [ ] 3.1 Add Download button to toolbar after Mark Sent button in BillingInvoicesView.vue
- [ ] 3.2 Configure button styling: variant="outlined", size="small", prepend-icon="mdi-download-circle-outline"
- [ ] 3.3 Add v-menu component to Download button with two menu items
- [ ] 3.4 Create menu item "Invoice PDF" with click handler
- [ ] 3.5 Create menu item "Delivery Note" with click handler

## 4. Frontend UI - Selection Logic and Enable/Disable

- [ ] 4.1 Add computed property `isDownloadEnabled` (enabled when exactly one invoice selected in checkbox mode)
- [ ] 4.2 Bind Download button `:disabled` property to `isDownloadEnabled`
- [ ] 4.3 Ensure Download button enables/disables in sync with Mark Sent button
- [ ] 4.4 Test button state transitions as invoices are selected/deselected

## 5. Frontend UI - Download Handlers

- [ ] 5.1 Create async function `handleDownloadInvoicePdf(externalInvoiceId: string)` 
- [ ] 5.2 Create async function `handleDownloadDeliveryNote(externalInvoiceId: string)`
- [ ] 5.3 Implement Blob-to-file-download mechanism using anchor element pattern
- [ ] 5.4 Add descriptive filenames based on invoice number (e.g., "invoice-{invoiceNumber}.pdf")
- [ ] 5.5 Add try-catch error handling that displays errors in errorMessage alert banner
- [ ] 5.6 Test download functions with network errors and API errors

## 6. Frontend UI - Error Handling

- [ ] 6.1 Ensure download errors display in existing errorMessage alert banner
- [ ] 6.2 Add user-friendly error messages for common failure scenarios:
       - Invoice not found (404)
       - Delivery note not available (404 for delivery note)
       - Invoice Ninja connectivity issues (503)
       - Generic network errors
- [ ] 6.3 Test error scenarios end-to-end

## 7. Component Testing

- [ ] 7.1 Write component tests for Download button rendering
- [ ] 7.2 Write component tests for button disabled/enabled state
- [ ] 7.3 Write component tests for v-menu dropdown appearance
- [ ] 7.4 Write component tests for menu item click handlers
- [ ] 7.5 Write component tests for error message display
- [ ] 7.6 Run all existing BillingInvoicesView tests to ensure no regressions

## 8. Integration Testing

- [ ] 8.1 Perform end-to-end test: select invoice → click Download → select Invoice PDF → file downloads
- [ ] 8.2 Perform end-to-end test: select invoice → click Download → select Delivery Note → file downloads
- [ ] 8.3 Test button disable state when multiple invoices selected
- [ ] 8.4 Test button disable state when no invoices selected
- [ ] 8.5 Test error case: Invoice Ninja returns 404
- [ ] 8.6 Test error case: Invoice Ninja returns 503 (connectivity error)
- [ ] 8.7 Test clearing selection removes enabled state from Download button

## 9. Documentation

- [ ] 9.1 Update API documentation for new endpoints in ops/BILLING_INTEGRATION.md
- [ ] 9.2 Update BillingController code comments with endpoint descriptions
- [ ] 9.3 Update billing.ts service function documentation with JSDoc comments
- [ ] 9.4 Add inline code comments for Blob download mechanism in BillingInvoicesView.vue

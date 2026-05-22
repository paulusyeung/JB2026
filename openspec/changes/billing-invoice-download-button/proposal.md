## Why

Invoice management requires quick access to supporting documents. Users currently cannot download invoice PDFs or delivery notes directly from the billing invoices list, forcing them to navigate to Invoice Ninja externally. Adding in-app download capability improves workflow efficiency and keeps users in the application context.

## What Changes

- Add a new "Download" button in the billing invoices toolbar, positioned after the "Mark Sent" button
- Button displays a dropdown menu with two options: "Invoice PDF" and "Delivery Note" when an invoice is selected
- Button remains disabled until exactly one invoice is selected (via checkbox mode)
- Clicking an option initiates a download of the requested document from Invoice Ninja through the backend proxy
- Downloads are triggered automatically via the browser's standard file download mechanism
- PDF files are named descriptively (e.g., `invoice-{invoiceNumber}.pdf`, `delivery-note-{invoiceNumber}.pdf`)

## Capabilities

### New Capabilities

- `invoice-download`: Download invoice PDF documents from Invoice Ninja via authenticated backend API endpoint
- `delivery-note-download`: Download delivery note PDF documents from Invoice Ninja via authenticated backend API endpoint

### Modified Capabilities

- `billing-invoice-list-ui`: Extended toolbar with new download dropdown button alongside existing action buttons

## Impact

**Frontend (Vue/TypeScript)**
- [BillingInvoicesView.vue](BillingInvoicesView.vue): Add Download button with dropdown menu, selection logic, download handlers
- `src/services/billing.ts`: Add `downloadInvoicePdf()` and `downloadDeliveryNote()` functions

**Backend (C#/.NET)**
- `BillingController.cs`: Add two new endpoints:
  - `GET /api/v2/billing/invoices/{id}/download/pdf` - Download invoice PDF
  - `GET /api/v2/billing/invoices/{id}/download/delivery-note` - Download delivery note
- These endpoints proxy requests to Invoice Ninja's document download API and return file streams

**API Contract**
- Both endpoints return HTTP 200 with file content (application/pdf)
- Error cases return standard billing error responses (4xx/5xx)

**User Experience**
- Buttons styled consistently with existing toolbar buttons (outlined, small, similar to "View" pattern)
- Dropdown menu appears on button click, similar to "Views" menu
- Downloads trigger automatically through browser; no additional UI needed
- Error messages displayed in alert banner if download fails

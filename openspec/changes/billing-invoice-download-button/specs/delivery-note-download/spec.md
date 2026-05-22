## ADDED Requirements

### Requirement: Download delivery note PDF
The system SHALL provide an authenticated endpoint that downloads the delivery note PDF document from Invoice Ninja for a given invoice ID.

#### Scenario: Successful delivery note PDF download
- **WHEN** a user clicks "Delivery Note" from the Download menu with exactly one invoice selected
- **THEN** the system requests the delivery note PDF from Invoice Ninja via the backend API endpoint
- **AND** returns the PDF file with `Content-Type: application/pdf` and `Content-Disposition: attachment`
- **AND** the browser automatically downloads the file to the user's downloads folder

#### Scenario: Invoice not found
- **WHEN** a user attempts to download a delivery note for an invoice ID that does not exist
- **THEN** the backend returns HTTP 404 with a billing error response
- **AND** the frontend displays an error message: "Invoice not found"

#### Scenario: Delivery note not available for invoice
- **WHEN** a user attempts to download a delivery note for an invoice that does not have a delivery note (e.g., invoice status does not support it)
- **THEN** the backend returns HTTP 404 or 400 from Invoice Ninja
- **AND** the frontend displays an error message: "Delivery note not available for this invoice"

#### Scenario: Download fails due to Invoice Ninja connectivity
- **WHEN** the backend cannot reach Invoice Ninja during the download request
- **THEN** the backend returns HTTP 503 or appropriate error status
- **AND** the frontend displays an error message: "Failed to download delivery note. Invoice Ninja is temporarily unavailable."

#### Scenario: Unauthenticated user attempts download
- **WHEN** an unauthenticated user attempts to download a delivery note PDF
- **THEN** the system returns HTTP 401 Unauthorized
- **AND** the frontend redirects to login

### Requirement: Backend endpoint for delivery note PDF download
The system SHALL expose a GET endpoint at `/api/v2/billing/invoices/{id}/download/delivery-note` that retrieves the delivery note PDF from Invoice Ninja and returns it as a file stream.

#### Scenario: Valid authenticated request
- **WHEN** a request is made to `GET /api/v2/billing/invoices/{invoiceId}/download/delivery-note` with valid JWT authentication
- **THEN** the backend proxies the request to Invoice Ninja's document download API
- **AND** returns the PDF stream with appropriate headers
- **AND** response code is HTTP 200

#### Scenario: Missing invoice ID parameter
- **WHEN** a request is made without the `{id}` path parameter
- **THEN** the system returns HTTP 400 Bad Request

#### Scenario: Invalid invoice ID format
- **WHEN** a request is made with an incorrectly formatted invoice ID
- **THEN** the system returns HTTP 400 Bad Request with message describing required format

### Requirement: Frontend service function for delivery note PDF download
The system SHALL provide a TypeScript service function `downloadDeliveryNote(externalInvoiceId: string)` that returns a Blob for the PDF file.

#### Scenario: Service function call succeeds
- **WHEN** `downloadDeliveryNote('INV-123')` is called
- **THEN** the function makes a fetch request to `/api/v2/billing/invoices/INV-123/download/delivery-note`
- **AND** returns a Promise<Blob> containing the PDF data

#### Scenario: Service function call fails
- **WHEN** `downloadDeliveryNote('INVALID')` is called and the backend returns an error
- **THEN** the function throws an error with the response status and message

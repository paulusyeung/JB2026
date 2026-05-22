## ADDED Requirements

### Requirement: Download invoice PDF
The system SHALL provide an authenticated endpoint that downloads the invoice PDF document from Invoice Ninja for a given invoice ID. The endpoint MUST handle Invoice Ninja's API requirement that PDF downloads use the invitation_key (not invoice ID). The backend SHALL extract the invitation_key from the invoice's invitations array before calling the Invoice Ninja download API.

#### Scenario: Successful invoice PDF download
- **WHEN** a user clicks "Invoice PDF" from the Download menu with exactly one invoice selected
- **THEN** the backend fetches the invoice details with `?include=invitations` parameter to extract the invitation_key
- **AND** the backend requests the PDF from Invoice Ninja using `GET /api/v1/invoice/{invitation_key}/download`
- **AND** returns the PDF file with `Content-Type: application/pdf` and `Content-Disposition: attachment`
- **AND** the browser automatically downloads the file to the user's downloads folder

#### Scenario: Invoice not found
- **WHEN** a user attempts to download a PDF for an invoice ID that does not exist
- **THEN** the backend returns HTTP 404 with a billing error response
- **AND** the frontend displays an error message: "Invoice not found"

#### Scenario: Invitation key not available
- **WHEN** the backend fetches the invoice but the invitations array is empty or unavailable
- **THEN** the backend returns HTTP 400 Bad Request
- **AND** the frontend displays an error message: "Invoice PDF is not available for this invoice"

#### Scenario: Download fails due to Invoice Ninja connectivity
- **WHEN** the backend cannot reach Invoice Ninja during the download request
- **THEN** the backend returns HTTP 503 or appropriate error status
- **AND** the frontend displays an error message: "Failed to download invoice. Invoice Ninja is temporarily unavailable."

#### Scenario: Unauthenticated user attempts download
- **WHEN** an unauthenticated user attempts to download an invoice PDF
- **THEN** the system returns HTTP 401 Unauthorized
- **AND** the frontend redirects to login

### Requirement: Backend endpoint for invoice PDF download
The system SHALL expose a GET endpoint at `/api/v2/billing/invoices/{id}/download/pdf` that retrieves the invoice PDF from Invoice Ninja and returns it as a file stream. The endpoint SHALL internally call Invoice Ninja's PDF download API (`GET /api/v1/invoice/{invitation_key}/download`) which requires extracting the invitation_key from the invoice's invitations array using `?include=invitations` query parameter.

#### Scenario: Valid authenticated request
- **WHEN** a request is made to `GET /api/v2/billing/invoices/{invoiceId}/download/pdf` with valid JWT authentication
- **THEN** the backend first fetches the invoice with `GET /api/v1/invoices/{invoiceId}?include=invitations`
- **AND** extracts the invitation_key from the first item in the invitations array
- **AND** proxies the request to `GET /api/v1/invoice/{invitation_key}/download` with `X-API-TOKEN` header
- **AND** returns the PDF stream with appropriate headers
- **AND** response code is HTTP 200

#### Scenario: Missing invoice ID parameter
- **WHEN** a request is made without the `{id}` path parameter
- **THEN** the system returns HTTP 400 Bad Request

#### Scenario: Invalid invoice ID format
- **WHEN** a request is made with an incorrectly formatted invoice ID
- **THEN** the system returns HTTP 400 Bad Request with message describing required format

### Requirement: Frontend service function for invoice PDF download
The system SHALL provide a TypeScript service function `downloadInvoicePdf(externalInvoiceId: string)` that returns a Blob for the PDF file.

#### Scenario: Service function call succeeds
- **WHEN** `downloadInvoicePdf('INV-123')` is called
- **THEN** the function makes a fetch request to `/api/v2/billing/invoices/INV-123/download/pdf`
- **AND** returns a Promise<Blob> containing the PDF data

#### Scenario: Service function call fails
- **WHEN** `downloadInvoicePdf('INVALID')` is called and the backend returns an error
- **THEN** the function throws an error with the response status and message

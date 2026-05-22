## MODIFIED Requirements

### Requirement: Manual billing invoices can be created through the backend Invoice Ninja proxy
JB2026 MUST allow users to create a billing invoice directly from the Billing Invoices dialog by submitting a normalized invoice payload to backend billing endpoints that create the invoice in Invoice Ninja.

#### Scenario: create manual invoice successfully
- **WHEN** the user saves a valid invoice from the dialog in `create` mode
- **THEN** the frontend posts the normalized invoice payload to a JB2026 billing endpoint
- **AND** the backend creates the invoice in Invoice Ninja
- **AND** the response includes normalized invoice summary/detail data for the saved invoice

#### Scenario: invalid invoice payload is rejected
- **WHEN** the user attempts to save an invoice with missing client selection, missing invoice date, or invalid line-item values
- **THEN** the backend rejects the request with a stable validation error
- **AND** the invoice is not created in Invoice Ninja

### Requirement: Existing draft invoices can be updated through the backend Invoice Ninja proxy
JB2026 MUST allow users to update an existing invoice from the Billing Invoices dialog only while the invoice remains in `Draft` status.

#### Scenario: update draft invoice successfully
- **WHEN** the user saves a valid invoice from the dialog in `edit` mode for an invoice that is still `Draft`
- **THEN** the backend updates the matching Invoice Ninja invoice
- **AND** returns normalized saved invoice data

#### Scenario: draft becomes non-draft before save
- **WHEN** the user opens a draft invoice for edit but the invoice is no longer `Draft` by the time save is processed
- **THEN** the backend rejects the update with a stable business error
- **AND** the saved state in Invoice Ninja is left unchanged

### Requirement: Invoice detail for dialog editing/view is loaded from backend-owned normalized data
JB2026 MUST provide a backend billing endpoint that returns normalized invoice detail for the shared billing invoice dialog.

#### Scenario: load invoice detail for dialog
- **WHEN** the user clicks an invoice number in Billing Invoices
- **THEN** the frontend requests invoice detail from a JB2026 billing endpoint
- **AND** the backend returns normalized invoice metadata, line items, and totals suitable for dialog display

#### Scenario: Invoice Ninja credentials remain backend-owned
- **WHEN** the frontend loads, creates, or updates invoice dialog data
- **THEN** all Invoice Ninja communication occurs through JB2026 backend endpoints
- **AND** no Invoice Ninja API key or secret is exposed to the web client
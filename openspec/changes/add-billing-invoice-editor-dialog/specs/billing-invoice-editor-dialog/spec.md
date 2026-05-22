## ADDED Requirements

### Requirement: Shared billing invoice dialog supports create, draft edit, and read-only view
The Billing Invoices experience MUST provide one shared dialog surface for invoice authoring and inspection. The dialog MUST open in `create` mode from the `New Invoice` toolbar button, in `edit` mode when the user clicks a `Draft` invoice number, and in `view` mode when the user clicks an invoice number whose status is not `Draft`.

#### Scenario: new invoice opens create dialog
- **WHEN** the user clicks `New Invoice` in `BillingInvoicesView`
- **THEN** the system opens the billing invoice dialog in `create` mode
- **AND** the dialog starts with empty invoice metadata and at least one blank line item row

#### Scenario: draft invoice opens editable dialog
- **WHEN** the user clicks an invoice number whose authoritative status is `Draft`
- **THEN** the system loads the invoice detail payload
- **AND** opens the shared dialog in `edit` mode
- **AND** enables editable fields and save actions

#### Scenario: non-draft invoice opens read-only dialog
- **WHEN** the user clicks an invoice number whose authoritative status is not `Draft`
- **THEN** the system opens the shared dialog in `view` mode
- **AND** displays the invoice values read-only
- **AND** does not allow save actions that mutate the invoice

### Requirement: Billing invoice dialog captures required invoice fields and line items
The shared dialog MUST allow the user to work with Invoice Ninja-backed invoice data including client selection, invoice date, job number, and repeated line items with `P.O.Number`, description, qty, unit, unit cost, line total, and invoice total.

#### Scenario: user edits invoice metadata
- **WHEN** the dialog is in `create` or `edit` mode
- **THEN** the user can select a client from Invoice Ninja-backed options
- **AND** pick an invoice date
- **AND** enter a job number

#### Scenario: user manages multiple line items
- **WHEN** the dialog is in `create` or `edit` mode
- **THEN** the user can add multiple line items
- **AND** each line item includes inputs for `P.O.Number`, description, qty, unit, and unit cost

#### Scenario: line totals and invoice total are calculated
- **WHEN** the user changes qty or unit cost on any line item
- **THEN** that line's total is recalculated as `qty * unit cost`
- **AND** the invoice total is recalculated as the sum of all line totals

#### Scenario: read-only mode preserves totals and line values
- **WHEN** the dialog is in `view` mode
- **THEN** all line items and totals remain visible
- **AND** the user cannot alter line values or invoice metadata

### Requirement: Invoice client selection is sourced from Invoice Ninja through the backend
Client selection in the billing invoice dialog MUST use Invoice Ninja-backed client options obtained through JB2026 backend billing endpoints, not hardcoded or locally persisted browser data.

#### Scenario: client options load for selector
- **WHEN** the user opens the billing invoice dialog in `create` or `edit` mode
- **THEN** the system requests client options through the backend billing API
- **AND** the selector displays Invoice Ninja client names/identifiers suitable for selection

#### Scenario: client search does not expose Invoice Ninja credentials
- **WHEN** the frontend requests client options
- **THEN** the request goes only to JB2026 backend endpoints
- **AND** no Invoice Ninja API token or secret is exposed to the browser

### Requirement: Dialog strings and validation are localization-ready
The billing invoice dialog MUST use localized text for labels, headers, validation messages, titles, and action buttons.

#### Scenario: localized dialog strings render
- **WHEN** the dialog is rendered in a supported locale
- **THEN** labels for client, invoice date, job number, line items, totals, save, cancel, and read-only mode use locale keys rather than hardcoded English strings

#### Scenario: localized validation message appears
- **WHEN** the user attempts to save with missing or invalid required data
- **THEN** the UI shows a localized validation message describing the problem
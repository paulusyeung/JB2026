## MODIFIED Requirements

### Requirement: Billing invoices toolbar with action buttons
The system SHALL display a toolbar with action buttons for managing billing invoices, including a `New Invoice` action that opens the shared billing invoice dialog and invoice-number interactions that reuse the same dialog.

#### Scenario: toolbar displays with existing buttons
- **WHEN** the user navigates to the Billing Invoices view
- **THEN** the toolbar displays the following buttons in order: Columns, Sorting, Check Box, Views, divider, New Invoice, Mark Sent, Download
- **AND** each button is styled consistently with the existing toolbar pattern

#### Scenario: new invoice button opens dialog instead of route navigation
- **WHEN** the user clicks `New Invoice`
- **THEN** the billing invoice dialog opens in `create` mode
- **AND** the user remains in `BillingInvoicesView`

#### Scenario: clicking invoice number opens shared dialog
- **WHEN** the user clicks an invoice number from the billing table or card view
- **THEN** the system opens the shared billing invoice dialog for that invoice
- **AND** the resulting dialog mode is `edit` for `Draft` invoices and `view` otherwise

#### Scenario: successful save refreshes list state
- **WHEN** the user successfully creates or updates an invoice from the shared dialog
- **THEN** the billing list refreshes or patches the affected invoice summary in place
- **AND** the visible list remains sorted and rendered according to the current view settings
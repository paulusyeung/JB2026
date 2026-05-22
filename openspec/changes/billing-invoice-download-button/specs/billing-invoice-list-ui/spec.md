## MODIFIED Requirements

### Requirement: Billing invoices toolbar with action buttons
The system SHALL display a toolbar with action buttons for managing billing invoices, including a new Download button alongside existing actions.

#### Scenario: Toolbar displays with all buttons
- **WHEN** the user navigates to the Billing Invoices view
- **THEN** the toolbar displays the following buttons in order: Columns, Sorting, Check Box, Views, divider, New Invoice, Mark Sent, Download
- **AND** each button is styled with variant="outlined" and size="small"

#### Scenario: Download button is disabled when no invoice selected
- **WHEN** the user first loads the Billing Invoices view or when checkbox mode is off
- **THEN** the Download button is disabled (:disabled=true)
- **AND** the button appears grayed out

#### Scenario: Download button is disabled when multiple invoices selected
- **WHEN** the user selects more than one invoice in checkbox mode
- **THEN** the Download button remains disabled
- **AND** the Mark Sent button also remains disabled (same logic)

#### Scenario: Download button is enabled when exactly one invoice selected
- **WHEN** the user selects exactly one invoice in checkbox mode
- **THEN** the Download button becomes enabled (:disabled=false)
- **AND** the button becomes clickable with normal styling

#### Scenario: Download menu appears on button click
- **WHEN** the user clicks the Download button while it is enabled
- **THEN** a v-menu dropdown appears below the button
- **AND** the menu displays two options: "Invoice PDF" and "Delivery Note"

#### Scenario: User clicks Invoice PDF menu option
- **WHEN** the user clicks the "Invoice PDF" menu item from the Download dropdown
- **THEN** the system calls the `downloadInvoicePdf()` service function with the selected invoice ID
- **AND** the menu closes
- **AND** the browser downloads the PDF file

#### Scenario: User clicks Delivery Note menu option
- **WHEN** the user clicks the "Delivery Note" menu item from the Download dropdown
- **THEN** the system calls the `downloadDeliveryNote()` service function with the selected invoice ID
- **AND** the menu closes
- **AND** the browser downloads the PDF file

#### Scenario: Download fails with error message
- **WHEN** a download request fails (e.g., Invoice Ninja unavailable, invoice not found)
- **THEN** the frontend displays an error message in the existing alert banner
- **AND** the error message is user-friendly and describes the failure reason
- **AND** the toolbar remains visible and functional

#### Scenario: Multiple invoices selected then selection reduced to one
- **WHEN** the user has two invoices selected and then deselects one
- **THEN** the Download button becomes enabled
- **AND** the button can be used immediately

#### Scenario: Single invoice selected then selection cleared
- **WHEN** the user has one invoice selected and then deselects it
- **THEN** the Download button becomes disabled
- **AND** the button appears grayed out

### Requirement: Download button styling consistency
The system SHALL style the Download button to match the existing toolbar button styling conventions.

#### Scenario: Download button has correct styling
- **WHEN** the Download button is rendered in the toolbar
- **THEN** it has variant="outlined"
- **AND** size="small"
- **AND** prepend-icon="mdi-download-circle-outline" (or similar download icon)
- **AND** text label is "Download"

#### Scenario: Download button tooltip (optional enhancement)
- **WHEN** the user hovers over a disabled Download button
- **THEN** optionally a tooltip could appear explaining "Select one invoice to download"
- **AND** this is a future enhancement, not required for initial release

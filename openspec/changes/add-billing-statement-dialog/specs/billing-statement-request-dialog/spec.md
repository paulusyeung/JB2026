## ADDED Requirements

### Requirement: Billing statement request dialog MUST present the required statement options and defaults
The system MUST present a billing statement request dialog for the selected client with a `Date Range` dropdown, a `Status` dropdown, selector controls for `Credits`, `Payments`, and `Aging`, plus `Cancel` and `Proceed` actions. The dialog MUST default `Date Range` to `All Outstanding`, MUST default `Status` to `All`, and MUST default all three selectors to unchecked.

#### Scenario: Dialog opens with required controls and default values
- **WHEN** the user opens the billing statement request dialog for a selected client
- **THEN** the dialog shows `Date Range`, `Status`, `Credits`, `Payments`, and `Aging` controls
- **AND** the `Date Range` value is `All Outstanding`
- **AND** the `Status` value is `All`
- **AND** the `Credits`, `Payments`, and `Aging` selectors are all unchecked
- **AND** the dialog shows `Cancel` and `Proceed` actions

#### Scenario: User can choose any supported date range preset
- **WHEN** the user expands the `Date Range` dropdown
- **THEN** the available options are `All Outstanding`, `This Month`, `Last Month`, `This Quarter`, and `This Year`

#### Scenario: User can choose any supported status preset
- **WHEN** the user expands the `Status` dropdown
- **THEN** the available options are `All`, `Paid`, and `Unpaid`

### Requirement: Billing statement request dialog MUST allow cancel without side effects
The system MUST let the user dismiss the statement request dialog with `Cancel` and MUST NOT trigger a statement request or open a new tab when the dialog is cancelled.

#### Scenario: Cancel closes the dialog without launching a statement
- **WHEN** the statement request dialog is open and the user clicks `Cancel`
- **THEN** the dialog closes
- **AND** the system MUST NOT send a statement request
- **AND** the system MUST NOT open a new browser tab

### Requirement: Proceed MUST forward the selected client and chosen options through the backend billing integration and open the resulting statement in a new tab
The system MUST submit the selected client's identifier together with the chosen date range, status, and selector values to a backend-owned billing statement endpoint. That backend endpoint MUST use Invoice Ninja `POST /api/v1/client_statement` for statement generation, mapping the selected client to `client_id`, the chosen date range to `start_date` and `end_date`, and the selector values to `show_credits_table`, `show_payments_table`, and `show_aging_table`. On success, the system MUST open the returned statement result in a new browser tab. While the request is in flight, the dialog MUST prevent duplicate `Proceed` submissions.

#### Scenario: Proceed submits the selected client and current dialog options
- **WHEN** the user clicks `Proceed` in the statement request dialog
- **THEN** the system sends the selected client's `externalClientId`, the selected `Date Range`, the selected `Status`, and the current `Credits`, `Payments`, and `Aging` selector values to the backend billing statement endpoint

#### Scenario: Backend maps request fields to the Invoice Ninja client statement API
- **WHEN** the backend processes a valid statement launch request
- **THEN** it calls Invoice Ninja `POST /api/v1/client_statement`
- **AND** it maps the selected client to `client_id`
- **AND** it maps the chosen date range to `start_date` and `end_date`
- **AND** it maps the dialog selectors to `show_credits_table`, `show_payments_table`, and `show_aging_table`

#### Scenario: Successful proceed opens the statement in a new tab
- **WHEN** the backend statement request succeeds
- **THEN** the application opens the generated billing statement in a new browser tab
- **AND** the current billing statement list remains open in the original tab

#### Scenario: Proceed cannot be submitted twice while launch is pending
- **WHEN** the user has already clicked `Proceed` and the statement launch request is still in flight
- **THEN** the dialog prevents a second `Proceed` submission until the first request completes

### Requirement: Statement launch failures MUST keep the user in the dialog and surface an error
If the backend billing statement request fails or cannot produce a launchable statement result, the system MUST keep the user on the current page, MUST surface an error in the dialog, and MUST NOT leave behind an unusable statement tab.

#### Scenario: Backend launch request fails
- **WHEN** the user clicks `Proceed` and the backend returns an error for the statement request
- **THEN** the dialog remains available in the current tab
- **AND** the system shows an error message for the failed launch
- **AND** the system MUST NOT navigate the current tab away from the billing statement list

#### Scenario: Requested status option cannot be mapped to the upstream client statement API
- **WHEN** the user clicks `Proceed` with a `Status` option that JB2026 cannot translate into a supported Invoice Ninja client statement request
- **THEN** the dialog remains available in the current tab
- **AND** the system shows a stable error explaining that the selected status option is not currently supported for statement generation
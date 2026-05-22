## ADDED Requirements

### Requirement: Mark Sent action is selection-gated
The Billing Invoices view MUST render a `Mark Sent` action after `New Invoice`. The action MUST be disabled by default and SHALL be enabled only when exactly one selected invoice has status `Draft`.

#### Scenario: no invoice selected
- **WHEN** the user has not selected any invoice in Billing Invoices
- **THEN** the `Mark Sent` action remains disabled

#### Scenario: one draft invoice selected
- **WHEN** the user selects exactly one invoice whose normalized status is `Draft`
- **THEN** the `Mark Sent` action becomes enabled

#### Scenario: selected invoice is not draft
- **WHEN** the user selects exactly one invoice whose normalized status is not `Draft`
- **THEN** the `Mark Sent` action remains disabled

#### Scenario: multiple invoices selected
- **WHEN** the user selects more than one invoice
- **THEN** the `Mark Sent` action remains disabled

### Requirement: Draft invoices can be sent through the backend billing proxy
When the user invokes `Mark Sent` for an eligible invoice, JB2026 MUST call a backend billing endpoint that instructs Invoice Ninja to send that draft invoice. The backend MUST reject invoices that are no longer in `Draft` status and MUST return a normalized billing summary on success.

#### Scenario: send draft invoice successfully
- **WHEN** the user clicks `Mark Sent` for an eligible draft invoice
- **THEN** JB2026 sends the request through its backend billing API
- **THEN** the backend instructs Invoice Ninja to transition the invoice to `Sent`
- **THEN** the response includes the updated invoice summary with status `Sent`

#### Scenario: selected invoice is stale
- **WHEN** the selected invoice is no longer `Draft` when the backend processes the request
- **THEN** the backend rejects the action with a stable business error
- **THEN** the invoice is not sent again

#### Scenario: Invoice Ninja send request fails
- **WHEN** Invoice Ninja rejects or cannot complete the send operation
- **THEN** JB2026 returns an error response without exposing Invoice Ninja credentials or raw secret values

### Requirement: Billing Invoices view refreshes immediately after send
After a successful send action, the Billing Invoices view MUST refresh the affected invoice state immediately so the user can see the updated status without manually reloading the page.

#### Scenario: table or card view reflects sent status
- **WHEN** the backend send action succeeds
- **THEN** the affected invoice row or card updates to show status `Sent`
- **THEN** the refreshed summary updates any returned sync timestamp shown in the list

#### Scenario: selection resets after send
- **WHEN** the invoice status refresh completes successfully
- **THEN** the sent invoice is no longer left in an actionable selected-draft state
- **THEN** the `Mark Sent` action returns to its disabled state until another eligible draft invoice is selected

#### Scenario: send action is pending
- **WHEN** the send request is in flight
- **THEN** the `Mark Sent` action is disabled to prevent duplicate submissions

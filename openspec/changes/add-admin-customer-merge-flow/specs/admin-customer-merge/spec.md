## ADDED Requirements

### Requirement: Admin customer merge action availability
The system MUST expose a persistent `Merge` action in Admin Customer management and MUST keep it disabled unless at least two distinct customers are selected.

#### Scenario: Merge action disabled without enough selection
- **WHEN** the user has selected fewer than two customers in `AdminCustomerView`
- **THEN** the `Merge` action remains visible and disabled

#### Scenario: Merge action enabled with valid multi-selection
- **WHEN** the user has selected two or more distinct customers in `AdminCustomerView`
- **THEN** the `Merge` action becomes enabled on every supported entry point for that view

### Requirement: Merge dialog enforces a single surviving target
The system MUST open a merge dialog that lists the selected customers and MUST allow the user to choose exactly one of those customers as the merge target before execution.

#### Scenario: Dialog lists only selected customers
- **WHEN** the user opens the merge dialog from `AdminCustomerView`
- **THEN** the dialog shows the currently selected customers as the only merge candidates

#### Scenario: Only one target can be chosen
- **WHEN** the user selects a target customer in the merge dialog
- **THEN** the dialog keeps exactly one customer marked as the surviving target at a time

### Requirement: Merge reassigns dependent customer references
When a merge is confirmed, the system MUST update every selected non-target customer's `InvoiceHeader.CustomerId` and `QtHeader.CustomerId` reference to the chosen target customer before source customers are retired.

The system MUST also update `JobOrder.CustomerName` from each selected non-target customer's exact customer name to the chosen target customer's name using a case-sensitive match.

#### Scenario: References move to the selected target
- **WHEN** the user confirms a merge with one target customer and one or more source customers
- **THEN** all `InvoiceHeader` and `QtHeader` rows that referenced the source customers reference the target customer after the merge completes

#### Scenario: Exact-case job order customer names are rewritten to the target name
- **WHEN** the user confirms a merge and one or more `JobOrder` rows have `CustomerName` exactly equal to a selected non-target customer's name
- **THEN** those `JobOrder.CustomerName` values are rewritten to the target customer's name after the merge completes
- **AND** `JobOrder` rows whose `CustomerName` differs only by letter casing remain unchanged

### Requirement: Merge retires non-target customers only
When a merge is confirmed, the system MUST leave the target customer unchanged and MUST retire each non-target customer in the merge set by setting `Customer.Retired = true`, `Customer.RetiredOn` to the execution date, and `Customer.RetiredBy` to the authenticated login user.

#### Scenario: Source customers are retired and target remains active
- **WHEN** the merge completes successfully
- **THEN** every non-target customer in the request is retired with retirement audit values
- **AND** the target customer remains unretired

### Requirement: Merge rejects invalid or stale selections
The system MUST reject merge requests that do not contain at least two distinct customers, whose target customer is not part of the selected set, or whose selected customers are missing or already retired.

#### Scenario: Request rejected when target is invalid
- **WHEN** the merge request target is not one of the selected customers
- **THEN** the system returns a validation error and performs no database updates

#### Scenario: Request rejected when selected customers are stale or retired
- **WHEN** one or more selected customers no longer exist or are already retired
- **THEN** the system rejects the merge request and performs no partial merge changes

### Requirement: Merge refreshes the admin customer view after completion
After a successful merge, the system MUST close the dialog, refresh the customer list, and update selection state so retired source customers are no longer selected.

#### Scenario: Successful merge refreshes visible state
- **WHEN** a merge request succeeds from `AdminCustomerView`
- **THEN** the dialog closes and the customer list reloads with source customers retired
- **AND** the surviving target customer remains the only selected customer when it is still present in the refreshed result set
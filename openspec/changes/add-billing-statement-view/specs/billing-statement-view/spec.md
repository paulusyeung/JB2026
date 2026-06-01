## ADDED Requirements

### Requirement: Billing statement view is available from Billing navigation
The system MUST expose a `Statement` entry under the Billing menu for authorized users and MUST route that entry to a dedicated billing statement list view that follows the `AdminCustomerView` layout pattern, color treatment, and i18n-ready structure.

#### Scenario: Billing menu shows statement entry
- **WHEN** an authorized Billing/Admin user opens the ClientApp navigation
- **THEN** the Billing menu includes a `Statement` entry alongside the existing Billing items

#### Scenario: Statement entry opens the list view
- **WHEN** the user navigates to the Billing `Statement` entry
- **THEN** the application renders the billing statement list view using the same list-first page structure and theme conventions established by `AdminCustomerView`

### Requirement: Statement list loads Invoice Ninja clients
The system MUST populate the billing statement list from Invoice Ninja client data exposed through the backend billing integration and MUST NOT reuse Admin Customer credential fields as visible statement columns.

#### Scenario: Statement list is sourced from Invoice Ninja clients
- **WHEN** the billing statement view loads successfully
- **THEN** the rows in the list represent Invoice Ninja clients returned by the billing API contract

#### Scenario: Credential columns are excluded from statement list
- **WHEN** the billing statement view renders its available columns
- **THEN** `Login Account` and `Password` are not present as statement columns

### Requirement: Outstanding balance is displayed with fixed statement formatting
The system MUST include an `Outstanding Balance` column in the billing statement list and MUST render each balance left-aligned with a leading `$`, comma thousands separators, and exactly two decimal places.

#### Scenario: Outstanding balance uses statement currency format
- **WHEN** a client row has an outstanding balance value of `1234.5`
- **THEN** the `Outstanding Balance` column displays `$1,234.50`

#### Scenario: Outstanding balance column remains left-aligned
- **WHEN** the statement list renders the `Outstanding Balance` column
- **THEN** the displayed balance text is left-justified within that column

### Requirement: Statement toolbar preserves base controls and gates the statement action by single selection
The system MUST preserve the first four toolbar controls and the divider from the baseline list pattern, MUST add a persistent `Statement` button after that divider, and MUST enable that button only when exactly one client is selected through checkbox selection.

#### Scenario: Statement action is disabled with no checked client
- **WHEN** no client is checked in the billing statement list
- **THEN** the `Statement` toolbar action remains visible and disabled

#### Scenario: Statement action is enabled with one checked client
- **WHEN** exactly one client is checked in the billing statement list
- **THEN** the `Statement` toolbar action becomes enabled

#### Scenario: Statement action is disabled with multiple checked clients
- **WHEN** more than one client is checked in the billing statement list
- **THEN** the `Statement` toolbar action remains disabled
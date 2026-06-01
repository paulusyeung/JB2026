## MODIFIED Requirements

### Requirement: Statement toolbar preserves base controls, gates the statement action by single selection, and launches the statement request dialog
The system MUST preserve the first four toolbar controls and the divider from the baseline list pattern, MUST add a persistent `Statement` button after that divider, MUST enable that button only when exactly one client is selected through checkbox selection, and MUST open the statement request dialog instead of showing a placeholder message when the enabled action is activated.

#### Scenario: Statement action is disabled with no checked client
- **WHEN** no client is checked in the billing statement list
- **THEN** the `Statement` toolbar action remains visible and disabled

#### Scenario: Statement action is enabled with one checked client
- **WHEN** exactly one client is checked in the billing statement list
- **THEN** the `Statement` toolbar action becomes enabled

#### Scenario: Statement action is disabled with multiple checked clients
- **WHEN** more than one client is checked in the billing statement list
- **THEN** the `Statement` toolbar action remains disabled

#### Scenario: Enabled statement action opens the request dialog
- **WHEN** exactly one client is checked and the user activates the enabled `Statement` toolbar action
- **THEN** the application opens the billing statement request dialog for that selected client
- **AND** the application MUST NOT show the previous follow-up placeholder message
## ADDED Requirements

### Requirement: Pending toolbar status actions are selection-gated
The pending schedule toolbar SHALL expose workflow-light and urgency-bell icon actions that are disabled by default and SHALL become enabled only when a selectable active row exists in the current pending list.

#### Scenario: No selected row keeps actions disabled
- **WHEN** the pending screen loads and no row is selected
- **THEN** all new workflow-light and urgency-bell action buttons are disabled

#### Scenario: Selecting a row enables actions
- **WHEN** the user selects a row in table, card, or mobile list mode
- **THEN** the toolbar enables all new workflow-light and urgency-bell action buttons

### Requirement: Workflow-light toolbar actions update selected-row step status
The system SHALL allow a user to click a workflow-light toolbar action and persist a workflow step status update for the selected order, then reflect the updated step status in the visible pending row.

#### Scenario: Workflow action succeeds
- **WHEN** a user with a selected row clicks a workflow-light action
- **THEN** the system sends a pending workflow update request for that selected order and step target
- **AND** updates the selected row workflow step value from the successful response

#### Scenario: Workflow action fails
- **WHEN** a user clicks a workflow-light action and the request fails
- **THEN** the selected row workflow step values remain unchanged
- **AND** the UI shows a user-visible error notice

### Requirement: Urgency-bell toolbar actions support legacy-compatible toggle behavior
The system SHALL allow red and yellow urgency-bell toolbar actions for the selected order, and clicking the currently active urgency color SHALL toggle the order back to neutral/default urgency.

#### Scenario: Set urgency bell color
- **WHEN** the selected row has neutral urgency and the user clicks bell red or bell yellow
- **THEN** the system persists that urgency color for the selected order
- **AND** the selected row urgency indicator updates to the chosen color

#### Scenario: Toggle off same urgency color
- **WHEN** the selected row already has urgency color red (or yellow) and the user clicks the same bell action again
- **THEN** the system persists neutral/default urgency for that selected order
- **AND** the selected row urgency indicator updates to neutral/default

### Requirement: Toolbar action icons align with pending status visual semantics
The pending toolbar action icons SHALL align with existing pending-list status semantics used by row rendering helpers, so users see consistent color/state meaning between toolbar controls and row indicators.

#### Scenario: Workflow icon semantic consistency
- **WHEN** workflow statuses are rendered in rows using the existing workflow status color mapping
- **THEN** the workflow toolbar actions use corresponding icon/color semantics for those same statuses

#### Scenario: Urgency icon semantic consistency
- **WHEN** urgency is rendered in rows using the existing urgency icon/color mapping
- **THEN** the urgency toolbar actions use corresponding bell icon/color semantics for red and yellow urgency

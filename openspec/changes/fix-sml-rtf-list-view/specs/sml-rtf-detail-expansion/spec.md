## ADDED Requirements

### Requirement: RTF invoice rows are expandable
Users SHALL be able to expand RTF header rows to view associated line item details. Rows that are not expanded SHALL display only master header information. Line item data is already loaded with the master data and is available immediately upon expansion.

#### Scenario: Expand row to show line items
- **WHEN** user clicks the expand arrow icon on an RTF header row
- **THEN** the row expands and displays line item details in a nested child table below the master row immediately (no loading delay)

#### Scenario: Collapse row to hide line items
- **WHEN** user clicks the collapse arrow icon on an expanded RTF header row
- **THEN** the row collapses and line item details are hidden

#### Scenario: Re-expand row shows data immediately
- **WHEN** user expands a row that was previously expanded and collapsed
- **THEN** line item data appears immediately without any loading state (data is already in memory)
### Requirement: Child table styling and layout
The child table (line items) SHALL use consistent row height and alignment to match legacy behavior. Child rows SHALL be visually distinct from master rows via background color and styling.

#### Scenario: Child rows have appropriate height
- **WHEN** line items are displayed in expanded child table
- **THEN** each child row has a minimum height of 32px to accommodate multi-line text wrapping

#### Scenario: Child table has distinct background
- **WHEN** a master row is expanded
- **THEN** the child table background is WhiteSmoke (light gray) to distinguish it from master rows

#### Scenario: Child table is read-only
- **WHEN** user views child table line items
- **THEN** line items are displayed in read-only mode (no editing, adding, or deleting allowed)

### Requirement: Expand/collapse state is tracked per row
Each RTF header row SHALL independently track whether it is expanded or collapsed. The expanded state SHALL be lost when navigating away from the page (no persistence required).

#### Scenario: Multiple rows can be expanded simultaneously
- **WHEN** user expands multiple RTF header rows
- **THEN** all expanded rows display their line items simultaneously without affecting other rows

#### Scenario: Expanded state is cleared on page reload
- **WHEN** user reloads the SML RTF List View page
- **THEN** all expanded rows are collapsed and expanded state is reset

#### Scenario: Expanded state is cleared on refresh
- **WHEN** user clicks the refresh button to reload master data
- **THEN** all expanded rows are collapsed and expanded state is reset

### Requirement: Empty line items are handled gracefully
If an RTF header has no associated line items, the expanded row SHALL display an appropriate empty state.

#### Scenario: Display empty state for headers with no line items
- **WHEN** user expands a row whose `items` array is empty
- **THEN** an appropriate message is displayed (e.g., \"No line items\") in the child table area

#### Scenario: Handle undefined items defensively
- **WHEN** user expands a row whose `items` property is undefined or null
- **THEN** the component handles this gracefully without errors, displaying an empty state
### Requirement: Keyboard navigation support
Users SHALL be able to navigate and expand/collapse rows using keyboard shortcuts.

#### Scenario: Space key expands/collapses row
- **WHEN** user presses Space key while focused on a row with expand arrow
- **THEN** the row toggles between expanded and collapsed state

#### Scenario: Arrow keys navigate between rows
- **WHEN** user presses Up/Down arrow keys
- **THEN** focus moves to the previous/next row in the master table


## ADDED Requirements

### Requirement: Invoice list supports expandable rows
The invoice list page SHALL support expandable/collapsible rows that reveal line item details when expanded.

#### Scenario: User can expand a row
- **WHEN** user clicks the expand icon on an invoice header row
- **THEN** the row expands to display a child table with line items

#### Scenario: User can collapse an expanded row
- **WHEN** user clicks the expand icon on an already expanded row
- **THEN** the row collapses and hides the line item details

#### Scenario: Multiple rows can be expanded simultaneously
- **WHEN** user expands multiple invoice header rows
- **THEN** all expanded rows remain visible with their line item details

### Requirement: Expand icon is displayed for each row
The master table SHALL display an expand/collapse icon for each invoice header row.

#### Scenario: Expand icon shows correct state
- **WHEN** a row is collapsed
- **THEN** the expand icon indicates the collapsed state (e.g., plus icon)

#### Scenario: Collapse icon shows correct state
- **WHEN** a row is expanded
- **THEN** the expand icon indicates the expanded state (e.g., minus icon)

### Requirement: Expanded state persists during filtering
The system SHALL preserve expanded row state when applying filters that do not change the underlying data.

#### Scenario: Expand state preserved during search
- **WHEN** user has rows expanded and applies a search filter
- **THEN** expanded rows remain expanded if they still appear in the filtered results

### Requirement: Refresh clears expanded state
The system SHALL clear all expanded rows when the user refreshes the data.

#### Scenario: Refresh collapses all rows
- **WHEN** user clicks the refresh button with rows expanded
- **THEN** all rows are collapsed after the data reloads

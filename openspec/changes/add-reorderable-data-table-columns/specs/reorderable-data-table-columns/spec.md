## Purpose

Describes how users arrange data-table columns in list views: reordering columns by dragging table headers, storing that order as a per-user preference that survives reloads and devices, and resetting back to the shipped default.

## ADDED Requirements

### Requirement: Users SHALL be able to reorder data-table columns by dragging header cells
The system SHALL allow a user to change the position of a data-table column by dragging its header cell onto another header cell, in every list view that offers a Columns menu, and SHALL re-render the grid in the new order.

#### Scenario: Column moves to the drop position
- **WHEN** a user drags a column header onto a different column header and releases it
- **THEN** the dragged column SHALL take the position of the target column and the remaining columns SHALL keep their relative order

#### Scenario: Drag over the same column is a no-op
- **WHEN** a user drags a column header and releases it over itself
- **THEN** the column order SHALL be unchanged

#### Scenario: Drag provides visual feedback
- **WHEN** a user is dragging a column header over a valid target
- **THEN** the system SHALL indicate the dragged source column and the prospective drop target

#### Scenario: Selection column is not reorderable
- **WHEN** checkbox mode is enabled and the row-selection header is displayed
- **THEN** the selection header SHALL NOT be draggable and SHALL NOT be displaced by reordering

#### Scenario: Only visible columns can be dropped onto
- **WHEN** a user drags a column header onto the header area
- **THEN** the drop SHALL be applied to a visible column and the resulting order SHALL be applied to the visible grid

#### Scenario: Card and mobile layouts are unaffected
- **WHEN** a view is displaying its card or mobile layout instead of the data table
- **THEN** no drag reorder affordance SHALL be shown and the stored order SHALL be preserved

### Requirement: Column order SHALL be stored as a per-user preference
The system SHALL store the user's column order per view and per user, and SHALL restore it whenever the view is loaded.

#### Scenario: Order survives a page reload
- **WHEN** a user reorders columns and the page is reloaded
- **THEN** the grid SHALL render the columns in the order the user chose

#### Scenario: Order is stored for the signed-in user
- **WHEN** a user reorders columns
- **THEN** the system SHALL persist the order as a preference owned by that user, so another user of the same application keeps their own order

#### Scenario: Order is available on another device
- **WHEN** a user signs in on a different browser or device after reordering columns
- **THEN** the grid SHALL render the stored order for that user

#### Scenario: Stored order is written without blocking the interaction
- **WHEN** a user reorders columns
- **THEN** the new order SHALL be applied immediately and SHALL be persisted in the background without a manual save step

#### Scenario: Preference write failure does not lose the order
- **WHEN** a preference cannot be written to the server
- **THEN** the reordered grid SHALL remain in place for the current session and SHALL be restored from local browser storage on the next load

#### Scenario: Views with no stored order use the shipped default
- **WHEN** a view is loaded by a user who has never reordered its columns or holds no stored preference
- **THEN** the grid SHALL render the columns in the shipped default order

#### Scenario: Columns that no longer exist are ignored
- **WHEN** a stored order contains a column key that the view no longer offers
- **THEN** the system SHALL ignore that key and render the remaining columns without error

#### Scenario: Newly added columns are appended
- **WHEN** a stored order does not contain a column that the view now offers
- **THEN** that column SHALL be rendered after the columns present in the stored order, in the view's default order

### Requirement: The Columns menu SHALL reflect the user's order and allow resetting it
The system SHALL list columns in the user's current order in the view's Columns menu and SHALL offer an action to restore the shipped default order.

#### Scenario: Menu lists columns in the current order
- **WHEN** a user opens the Columns menu
- **THEN** the listed columns SHALL appear in the same order as the rendered grid

#### Scenario: Reset restores the default order
- **WHEN** a user invokes the reset action after reordering columns
- **THEN** the grid SHALL render the shipped default column order and the reset SHALL be persisted like any other change

#### Scenario: Column visibility toggles keep working
- **WHEN** a user hides or shows a column from the Columns menu
- **THEN** the visibility change SHALL be applied and persisted independently of the column order

### Requirement: In a master-detail view, only the master grid SHALL be reorderable
A view that renders a nested grid inside its expanded rows SHALL offer reordering on the master grid only, and SHALL leave the nested grid's header rendering untouched.

#### Scenario: Master grid is reorderable and persisted
- **WHEN** a user reorders columns of a master-detail view's master grid
- **THEN** the master grid SHALL re-render in the new order, the order SHALL be persisted as that view's per-user preference, and the Columns menu's reset action SHALL restore the master's default order

#### Scenario: Nested grid is not a drag surface
- **WHEN** a user expands a row in a master-detail view
- **THEN** the nested grid's headers SHALL render exactly as before, SHALL show no drag affordance, and SHALL NOT be displaceable

#### Scenario: Nested grid column visibility is unchanged
- **WHEN** a user opens the master-detail view's Columns menu
- **THEN** the menu SHALL continue to list and toggle the nested grid's columns as it did before reordering was added, and its reorderable-keys SHALL NOT be applied to that grid

#### Scenario: Master grid utility columns are pinned
- **WHEN** a view's master grid leads with non-data columns such as a row expander or a row-number column
- **THEN** those columns SHALL NOT be draggable and SHALL NOT be displaced by a drop onto them

#### Scenario: Expanded rows still span the full grid
- **WHEN** the master grid is rendered with reorderable headers and a row is expanded
- **THEN** the expanded row SHALL continue to span all master columns, including the selection column when checkbox mode is enabled

### Requirement: Reorderable headers SHALL preserve existing table behavior
Rendering reorderable header cells SHALL NOT change the visible or interactive behavior of the data table.

#### Scenario: Sorting from a header still works
- **WHEN** a user clicks or keyboard-activates a sortable column header
- **THEN** the grid SHALL sort by that column exactly as it did before headers became reorderable

#### Scenario: Select-all still works
- **WHEN** a user activates the select-all control in the header while checkbox mode is enabled
- **THEN** all selectable rows SHALL become selected

#### Scenario: Header layout is unchanged
- **WHEN** the grid is rendered
- **THEN** column widths, alignment, the sticky header position, and icon-only headers with their accessible names SHALL match the previous rendering

#### Scenario: Custom header cells are retained
- **WHEN** a view ships a header cell with custom content and that view gains reorderable headers
- **THEN** the custom cell SHALL keep its content, tooltip and accessible name, and SHALL remain reorderable like any other column

#### Scenario: Every converted view presents the same affordance
- **WHEN** reordering has been added to more than one view
- **THEN** each view SHALL present the same drag affordance, the same drop feedback and the same excluded cells, so the interaction is identical across views, and a change to the affordance in one view SHALL change it in all of them

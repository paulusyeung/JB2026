## ADDED Requirements

### Requirement: Views dropdown button exists in toolbar
The JobList toolbar SHALL display a "Views" dropdown button (with `mdi-eye-outline` icon) that reveals a menu with two options: Detail View (table) and Card View (cards).

#### Scenario: User sees Views button in desktop toolbar
- **WHEN** the user views the JobList page on a desktop screen (above phone breakpoint)
- **THEN** a "Views" dropdown button is visible in the toolbar bar

#### Scenario: User sees Views option in mobile overflow menu
- **WHEN** the user views the JobList page on a phone screen (at or below phone breakpoint)
- **THEN** the Views options appear in the overflow (more actions) menu

### Requirement: Detail view shows table layout
When the view mode is set to `detail`, the JobList SHALL render the data in a `v-data-table` component (the existing table layout).

#### Scenario: Default view is detail (table)
- **WHEN** the user loads the JobList page for the first time with no persisted settings
- **THEN** the table layout is displayed

#### Scenario: Switching to detail view shows table
- **WHEN** the user selects "Detail View" from the Views menu
- **THEN** the table layout is displayed

### Requirement: Card view shows card layout
When the view mode is set to `card`, the JobList SHALL render the data in a card grid layout (reusing the existing mobile card template).

#### Scenario: Switching to card view shows cards
- **WHEN** the user selects "Card View" from the Views menu
- **THEN** the card grid layout is displayed with job order cards

#### Scenario: Card layout works on desktop
- **WHEN** the user selects "Card View" on a desktop screen
- **THEN** the cards are displayed in a multi-column grid (not a single-column stack)

### Requirement: View mode preference is persisted
The selected view mode SHALL be persisted using the `useViewSettings` composable so that it survives page reloads.

#### Scenario: View mode persists across reload
- **WHEN** the user switches to card view and then reloads the page
- **THEN** the card view is still active after reload

#### Scenario: View mode persists across navigation
- **WHEN** the user switches to card view, navigates away, and returns to JobList
- **THEN** the card view is still active

### Requirement: Popup button is removed
The standalone "Popup" button SHALL no longer appear in the JobList toolbar or overflow menu.

#### Scenario: Popup button absent in desktop toolbar
- **WHEN** the user views the JobList page on a desktop screen
- **THEN** no "Popup" button is visible in the toolbar

#### Scenario: Popup option absent in mobile overflow menu
- **WHEN** the user views the JobList page on a phone screen and opens the overflow menu
- **THEN** no "Popup" option is listed in the menu

### Requirement: Row and card click still opens editor
Clicking a row in table view or a card in card view SHALL open the job order editor dialog for the clicked item.

#### Scenario: Clicking table row opens editor
- **WHEN** the user clicks a row in table view
- **THEN** the job order editor dialog opens for that order

#### Scenario: Clicking card opens editor
- **WHEN** the user clicks a card in card view
- **THEN** the job order editor dialog opens for that order

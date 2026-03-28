## ADDED Requirements

### Requirement: All DevExpress UI Components Must Be Removed from the Runtime Distribution
No DevExpress packages MAY be referenced in the Vue 3 SPA or any distributed assembly. Replacements must be MIT or Apache 2.0 licensed.

#### Scenario: Dependency scan finds no DevExpress packages
- **WHEN** the CI pipeline runs a dependency licence scan on the built SPA
- **THEN** zero DevExpress package references SHALL be present in `package.json` or the production bundle

### Requirement: Vuetify 3 Data Tables Must Support Sorting, Filtering, and Pagination
Every migrated data grid view MUST support column sort, client or server-side filtering, and pagination via the Vuetify 3 `v-data-table-server` or equivalent component.

#### Scenario: User sorts a column in a migrated grid view
- **WHEN** a user clicks a column header in a Vuetify 3 data table
- **THEN** the rows SHALL be re-ordered by that column without a full page reload

### Requirement: FullCalendar Must Replace DevExpress Scheduler
The legacy DevExpress resource scheduler view MUST be replaced with FullCalendar (Apache 2.0) with equivalent drag-and-drop event editing functionality.

#### Scenario: User drags an event to a new time slot in FullCalendar
- **WHEN** a user drags a calendar event to a new slot
- **THEN** the event time SHALL update and an API call SHALL persist the change

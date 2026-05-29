## ADDED Requirements

### Requirement: Stateful ClientApp views SHALL guard nullable and indexed data access
ClientApp forms, lists, and scheduler flows MUST guard nullable values, optional properties, and indexed collection access before reading or writing dependent state.

#### Scenario: first ship-to address is used to seed selected state
- **WHEN** a form derives selected values from the first element of a potentially empty address list
- **THEN** the code verifies the element exists before dereferencing nested properties
- **AND** provides a safe fallback state when the list is empty

#### Scenario: scheduler row is updated after async action
- **WHEN** a schedule view updates a row by index after an async workflow result returns
- **THEN** the code verifies the row still exists and preserves required fields
- **AND** does not assign partially undefined objects into strictly typed row state

### Requirement: View and store contracts SHALL use declared properties only
ClientApp views MUST consume store and component contracts using properties and exports that are actually declared by the owning module.

#### Scenario: view watches theme state
- **WHEN** a view reads or watches a theme store value
- **THEN** it uses a declared store property such as the documented mode or computed state
- **AND** does not reference undeclared properties that compile incorrectly or fail at runtime

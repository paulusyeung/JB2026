## ADDED Requirements

### Requirement: View settings are persisted to the server
The system SHALL persist user view settings (visible columns, sorting, checkbox mode, and view mode) to the server's UserPreference table.

#### Scenario: Save view settings to server
- **WHEN** a user modifies any view setting (e.g., toggles a column, changes sort order)
- **THEN** the system sends a PUT request to `/api/v2/user-preferences/{objectType}/{objectId}` with the updated settings as JSON metadata

#### Scenario: Load view settings from server on mount
- **WHEN** a view component mounts (e.g., StockView)
- **THEN** the system sends a GET request to `/api/v2/user-preferences/{objectType}/{objectId}` and applies the returned settings

#### Scenario: Existing localStorage data is preserved as fallback
- **WHEN** no server record exists for a view
- **THEN** the system falls back to the existing localStorage data under the key `view-settings-{viewId}`

#### Scenario: Server settings override localStorage
- **WHEN** both server and localStorage contain settings for a view
- **THEN** the server settings take precedence over localStorage

### Requirement: View settings use static GUID identifiers
The system SHALL use static GUID values as ObjectId identifiers for each view, defined in a constants file.

#### Scenario: Each view has a unique static GUID
- **WHEN** a view is registered in the constants file
- **THEN** it is assigned a unique, fixed GUID that does not change across sessions

#### Scenario: ObjectType uses integer constant on client
- **WHEN** the client identifies the preference type
- **THEN** it uses the integer constant `1` for view settings

### Requirement: View settings save is debounced
The system SHALL debounce save operations to reduce API call frequency during rapid UI interactions.

#### Scenario: Rapid changes trigger debounced save
- **WHEN** a user makes multiple view setting changes within 500ms
- **THEN** only one API save is triggered after the debounce period

#### Scenario: localStorage is updated on every change
- **WHEN** a view setting changes
- **THEN** the localStorage value is updated immediately regardless of the debounce timer

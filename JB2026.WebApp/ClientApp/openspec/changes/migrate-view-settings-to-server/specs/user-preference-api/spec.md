## ADDED Requirements

### Requirement: API endpoint retrieves user preference
The system SHALL provide a GET endpoint to retrieve a user's preference for a specific object.

#### Scenario: Retrieve existing preference
- **WHEN** a GET request is made to `/api/v2/user-preferences/{objectType}/{objectId}`
- **THEN** the system returns the stored JSON metadata in a `{ "metadata": "..." }` response

#### Scenario: Retrieve non-existent preference
- **WHEN** a GET request is made to `/api/v2/user-preferences/{objectType}/{objectId}` and no record exists
- **THEN** the system returns `{ "metadata": null }` with HTTP 200

#### Scenario: Unauthenticated request is rejected
- **WHEN** an unauthenticated request is made to the endpoint
- **THEN** the system returns HTTP 401

### Requirement: API endpoint saves user preference
The system SHALL provide a PUT endpoint to save or update a user's preference for a specific object.

#### Scenario: Save new preference
- **WHEN** a PUT request is made to `/api/v2/user-preferences/{objectType}/{objectId}` with JSON metadata
- **THEN** the system creates a new UserPreference record and returns HTTP 200

#### Scenario: Update existing preference
- **WHEN** a PUT request is made to `/api/v2/user-preferences/{objectType}/{objectId}` with updated JSON metadata
- **THEN** the system updates the existing UserPreference record and returns HTTP 200

#### Scenario: Save requires authentication
- **WHEN** an unauthenticated request is made to the endpoint
- **THEN** the system returns HTTP 401

### Requirement: Preference is scoped to user and object
The system SHALL scope preferences to the authenticated user and the specified object identifier.

#### Scenario: User can only access own preferences
- **WHEN** User A requests a preference for an object
- **THEN** the system only returns preferences where `UserId` matches User A's identity

#### Scenario: Different users have separate preferences
- **WHEN** User A and User B both save preferences for the same object
- **THEN** each user's preferences are stored and retrieved independently

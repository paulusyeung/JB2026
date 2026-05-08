## ADDED Requirements

### Requirement: Automatic access token refresh on 401
The frontend SHALL automatically attempt to refresh the access token when receiving an HTTP 401 response from the API.

#### Scenario: Successful auto-refresh
- **WHEN** an API request receives a 401 response
- **AND** a valid refresh token is stored in the session
- **THEN** the system calls the refresh endpoint to obtain a new access token
- **AND** the original request is retried with the new access token
- **AND** the user remains authenticated without intervention

#### Scenario: Auto-refresh fails
- **WHEN** an API request receives a 401 response
- **AND** the refresh token is invalid or expired
- **THEN** the system clears the session and redirects to the login page
- **AND** the original request fails with an authentication error

### Requirement: Concurrent request handling during refresh
The frontend SHALL queue concurrent requests while a token refresh is in progress to prevent multiple refresh attempts.

#### Scenario: Multiple concurrent 401s
- **WHEN** multiple API requests receive 401 responses simultaneously
- **THEN** only one refresh request is sent to the server
- **AND** all queued requests wait for the refresh to complete
- **AND** all queued requests are retried with the new access token

### Requirement: Refresh token storage
The frontend SHALL store the refresh token in `localStorage` when `keepMeSignedIn` is true.

#### Scenario: Store refresh token
- **WHEN** login succeeds with `keepMeSignedIn: true`
- **THEN** the refresh token is stored in `localStorage` with key `jb2026.refreshToken`
- **AND** the access token is stored as before

#### Scenario: No refresh token stored
- **WHEN** login succeeds with `keepMeSignedIn: false`
- **THEN** no refresh token is stored in `localStorage`
- **AND** only the access token is stored

### Requirement: Logout clears refresh token
The frontend SHALL remove the refresh token from storage on logout.

#### Scenario: Logout removes tokens
- **WHEN** user calls the logout function
- **THEN** both the access token and refresh token are removed from `localStorage`
- **AND** the refresh token is revoked via the backend API before clearing local storage

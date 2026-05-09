## MODIFIED Requirements

### Requirement: Refresh token exchange
The system SHALL provide an endpoint `POST /api/v2/auth/refresh` to exchange a valid refresh token for a new access token and a new refresh token. Token validation and consumption SHALL be atomic to prevent race conditions.

#### Scenario: Refresh request contract
- **WHEN** client calls `/api/v2/auth/refresh`
- **THEN** request body contains `refreshToken` as a string field

#### Scenario: Successful refresh
- **WHEN** client sends a valid, unused refresh token to `/api/v2/auth/refresh`
- **THEN** the system returns a new `accessToken` and a new `refreshToken`
- **AND** the old refresh token is atomically validated and removed in a single operation

#### Scenario: Refresh with expired token
- **WHEN** client sends a refresh token that has expired
- **THEN** the system returns HTTP 401 Unauthorized
- **AND** response payload contains `error: "invalid_refresh_token"`
- **AND** the client must re-authenticate with username and password

#### Scenario: Refresh with already-used token
- **WHEN** client sends a refresh token that was already exchanged
- **THEN** the system returns HTTP 401 Unauthorized
- **AND** the token is unusable (already removed from store)

### Requirement: Refresh token rotation
The system SHALL rotate refresh tokens on each successful exchange using atomic validate-and-consume semantics.

#### Scenario: Token rotation on exchange
- **WHEN** a refresh token is successfully exchanged for a new access token
- **THEN** the old refresh token is atomically removed from the store during validation
- **AND** a new refresh token is issued with the same expiration window
- **AND** no race condition exists between validation and removal

#### Scenario: Concurrent refresh attempts with same token
- **WHEN** two requests simultaneously attempt to use the same refresh token
- **THEN** only one request succeeds (the one whose atomic remove wins)
- **AND** the other request fails with HTTP 401 Unauthorized
- **AND** no false theft detection occurs

## ADDED Requirements

### Requirement: Atomic validate-and-consume operation
The refresh token service SHALL provide an atomic operation that validates and consumes a refresh token in a single thread-safe step.

#### Scenario: Atomic validation and consumption
- **WHEN** `ValidateAndConsumeAsync` is called with a valid, unused token
- **THEN** the token is validated and removed from the store atomically
- **AND** the associated user ID is returned

#### Scenario: Atomic operation prevents reuse
- **WHEN** `ValidateAndConsumeAsync` is called with a token that was already consumed
- **THEN** the operation returns null (token not found)
- **AND** no race condition exists between validation and removal

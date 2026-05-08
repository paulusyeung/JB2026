## ADDED Requirements

### Requirement: Refresh token issuance on login
The system SHALL issue a refresh token alongside the access token when the user checks "Keep Me Signed In" during login.

#### Scenario: Login with Keep Me Signed In
- **WHEN** user submits login with `keepMeSignedIn: true`
- **THEN** the response includes both `accessToken` and `refreshToken`
- **AND** the refresh token has a lifetime of 30 days (configurable via `Jwt:RefreshTokenExpiryDays`)

#### Scenario: Login without Keep Me Signed In
- **WHEN** user submits login with `keepMeSignedIn: false` (or omitted)
- **THEN** the response includes only `accessToken` and no `refreshToken`
- **AND** behavior is identical to the current implementation

### Requirement: Refresh token exchange
The system SHALL provide an endpoint `POST /api/v2/auth/refresh` to exchange a valid refresh token for a new access token and a new refresh token.

#### Scenario: Refresh request contract
- **WHEN** client calls `/api/v2/auth/refresh`
- **THEN** request body contains `refreshToken` as a string field

#### Scenario: Successful refresh
- **WHEN** client sends a valid, unused refresh token to `/api/v2/auth/refresh`
- **THEN** the system returns a new `accessToken` and a new `refreshToken`
- **AND** the old refresh token is invalidated

#### Scenario: Refresh with expired token
- **WHEN** client sends a refresh token that has expired
- **THEN** the system returns HTTP 401 Unauthorized
- **AND** response payload contains `error: "invalid_refresh_token"`
- **AND** the client must re-authenticate with username and password

#### Scenario: Refresh with already-used token
- **WHEN** client sends a refresh token that was already exchanged
- **THEN** the system returns HTTP 401 Unauthorized
- **AND** all refresh tokens for that user are revoked (token theft detection)

### Requirement: Refresh token revocation
The system SHALL provide an endpoint `POST /api/v2/auth/revoke` to invalidate a refresh token explicitly (e.g., on logout).

#### Scenario: Revoke request contract
- **WHEN** client calls `/api/v2/auth/revoke`
- **THEN** request body contains `refreshToken` as a string field

#### Scenario: Successful revocation
- **WHEN** client sends a valid refresh token to `/api/v2/auth/revoke`
- **THEN** the refresh token is invalidated
- **AND** subsequent attempts to use it return HTTP 401

#### Scenario: Revoke already-invalid token
- **WHEN** client sends an already-invalid or unknown refresh token to `/api/v2/auth/revoke`
- **THEN** the system returns HTTP 204 No Content (idempotent)

### Requirement: Refresh token rotation
The system SHALL rotate refresh tokens on each successful exchange to detect unauthorized usage.

#### Scenario: Token rotation on exchange
- **WHEN** a refresh token is successfully exchanged for a new access token
- **THEN** the old refresh token is marked as used/invalid
- **AND** a new refresh token is issued with the same expiration window

### Requirement: Refresh token configuration
The system SHALL read the refresh token expiration period from configuration with a default of 30 days.

#### Scenario: Default expiration
- **WHEN** `Jwt:RefreshTokenExpiryDays` is not configured
- **THEN** refresh tokens expire after 30 days

#### Scenario: Custom expiration
- **WHEN** `Jwt:RefreshTokenExpiryDays` is set to a custom value (e.g., 7)
- **THEN** refresh tokens expire after that many days

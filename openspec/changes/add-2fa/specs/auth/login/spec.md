## Purpose

Defines the login flow for the JB2026 application, covering credential verification, two-factor authentication (when enabled), and token issuance. This capability is modified from the original single-step login to support a two-step flow when the user has 2FA enabled.

## ADDED Requirements

### Requirement: Login supports two-step authentication when 2FA is enabled

The system SHALL support a two-step login flow: first verify username and password, then verify the TOTP code if the user has 2FA enabled. The first step SHALL return a temporary token that is valid only for completing the second step.

#### Scenario: User without 2FA logs in
- **WHEN** a user submits valid username and password and 2FA is not enabled
- **THEN** the system returns a standard TokenResponse with access token, refresh token (if KeepMeSignedIn), and user profile

#### Scenario: User with 2FA logs in
- **WHEN** a user submits valid username and password and 2FA is enabled
- **THEN** the system returns a response with `requires2fa: true` and a temporary `twoFactorToken` (valid for 5 minutes) instead of an access token

#### Scenario: Invalid credentials during first step
- **WHEN** a user submits invalid username or password
- **THEN** the system returns 401 Unauthorized (same as current behavior)

### Requirement: Temporary 2FA token is single-use and short-lived

The temporary token issued after successful password verification SHALL be valid for 5 minutes and SHALL be consumed upon use. It SHALL NOT be usable as an access token.

#### Scenario: Temporary token used within validity window
- **WHEN** a user submits a valid TOTP code with a valid temporary token
- **THEN** the temporary token is consumed and a full TokenResponse is issued

#### Scenario: Temporary token expired
- **WHEN** a user submits a TOTP code with an expired temporary token
- **THEN** the system returns 401 Unauthorized and the user must restart the login flow

#### Scenario: Temporary token reused
- **WHEN** a user submits a TOTP code with an already-consumed temporary token
- **THEN** the system returns 401 Unauthorized

### Requirement: TOTP code verification during login

The system SHALL accept either a TOTP code or a recovery code during the second step of login. The same rate-limiting rules apply as in the 2FA verification requirement.

#### Scenario: Valid TOTP code completes login
- **WHEN** a user provides a valid TOTP code with a valid temporary token
- **THEN** the system returns a full TokenResponse with access token, refresh token (if KeepMeSignedIn), and user profile

#### Scenario: Valid recovery code completes login
- **WHEN** a user provides a valid recovery code with a valid temporary token
- **THEN** the system consumes the recovery code, returns a full TokenResponse, and the user remains logged in with 2FA still active

#### Scenario: Invalid TOTP code during login
- **WHEN** a user provides an invalid TOTP code with a valid temporary token
- **THEN** the system returns an error indicating the code is invalid and the temporary token remains valid for retry (up to rate limit)

### Requirement: Frontend handles two-step login flow

The frontend login view SHALL detect when `requires2fa` is returned and transition to a TOTP input screen. The temporary token SHALL be stored in memory (not localStorage) for the duration of the second step.

#### Scenario: Frontend receives requires2fa response
- **WHEN** the login API returns `requires2fa: true`
- **THEN** the frontend hides the username/password form and displays a 6-digit code input field with a "Verify" button

#### Scenario: Frontend successfully verifies 2FA code
- **WHEN** the user enters a valid 6-digit code and the API returns a full TokenResponse
- **THEN** the frontend stores the access token and refresh token (same as current behavior) and navigates to the intended page

#### Scenario: Frontend 2FA verification fails
- **WHEN** the user enters an invalid code and the API returns an error
- **THEN** the frontend displays the error message and allows the user to retry (until rate-limited)

#### Scenario: Frontend 2FA timeout
- **WHEN** the temporary token expires (5 minutes)
- **THEN** the frontend displays an error and returns the user to the username/password form

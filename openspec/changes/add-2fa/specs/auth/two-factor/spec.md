## Purpose

Provides TOTP-based two-factor authentication so that users can protect their accounts with a second factor beyond password. Includes enrollment (secret generation, QR code, recovery codes), verification during login, and admin-initiated disable.

## ADDED Requirements

### Requirement: User can enable 2FA from their profile dialog

The system SHALL display a "Two-Factor Authentication" section within the user profile dialog (opened from the topbar by clicking the user's name). This section SHALL show the current 2FA status (enabled or disabled) and provide actions to enable or disable 2FA. Enabling 2FA is entirely opt-in — the user must explicitly choose to enable it.

#### Scenario: User views 2FA status in profile dialog
- **WHEN** an authenticated user opens their profile dialog from the topbar
- **THEN** the dialog displays a "Two-Factor Authentication" section showing whether 2FA is currently enabled or disabled

#### Scenario: User with 2FA disabled clicks Enable
- **WHEN** a user with 2FA disabled clicks the "Enable 2FA" button in the profile dialog
- **THEN** the system generates a TOTP secret, stores it encrypted, and displays a QR code with a provisioning URI for scanning with an authenticator app (Google Authenticator, Authy, etc.)

#### Scenario: User with 2FA enabled clicks Disable
- **WHEN** a user with 2FA enabled clicks the "Disable 2FA" button in the profile dialog
- **THEN** the system prompts for their password and a TOTP code (or recovery code) before deactivating 2FA

#### Scenario: Initiate 2FA enrollment
- **WHEN** an authenticated user requests 2FA enrollment
- **THEN** the system generates a TOTP secret, stores it encrypted, and returns a provisioning URI compatible with standard authenticator apps (Google Authenticator, Authy, etc.)

#### Scenario: User already has 2FA enabled
- **WHEN** an authenticated user requests 2FA enrollment and 2FA is already enabled on their account
- **THEN** the system returns an error indicating 2FA is already active

### Requirement: User must verify TOTP code to complete enrollment

The system SHALL require the user to provide a valid TOTP code before marking 2FA as active. This prevents the user from being locked out due to a misconfigured authenticator app.

#### Scenario: Successful verification completes enrollment
- **WHEN** the user provides a valid TOTP code during enrollment
- **THEN** 2FA is marked as active for their account and the system generates a set of recovery codes

#### Scenario: Invalid TOTP code rejects enrollment
- **WHEN** the user provides an invalid TOTP code during enrollment
- **THEN** the system returns an error and 2FA remains inactive

### Requirement: User receives recovery codes at enrollment

The system SHALL generate a set of one-time recovery codes when 2FA enrollment is completed. Recovery codes SHALL be displayed to the user once and then stored hashed. Each recovery code SHALL be usable exactly once.

#### Scenario: Recovery codes are generated
- **WHEN** 2FA enrollment is completed with a valid TOTP code
- **THEN** the system generates 10 recovery codes and returns them to the user in plaintext

#### Scenario: Recovery codes are stored securely
- **WHEN** recovery codes are generated
- **THEN** the codes are stored as salted hashes in the database, not in plaintext

### Requirement: User can disable 2FA

The system SHALL allow a user to disable 2FA by providing their current password and a valid TOTP code (or a recovery code). This prevents an attacker with session access from disabling 2FA.

#### Scenario: Disable with password and TOTP code
- **WHEN** a user provides their password and a valid TOTP code to disable 2FA
- **THEN** 2FA is deactivated and all existing recovery codes are invalidated

#### Scenario: Disable with password and recovery code
- **WHEN** a user provides their password and a valid recovery code to disable 2FA
- **THEN** 2FA is deactivated, the used recovery code is consumed, and all remaining recovery codes are invalidated

#### Scenario: Invalid credentials reject disable
- **WHEN** a user provides an invalid password or invalid TOTP/recovery code to disable 2FA
- **THEN** 2FA remains active and the system returns an error

### Requirement: Admin can disable 2FA for any user

The system SHALL allow administrators to disable 2FA for any user without requiring the user's password or TOTP code. This provides account recovery when a user loses access to their authenticator device and recovery codes.

#### Scenario: Admin disables 2FA for a user
- **WHEN** an administrator requests 2FA disable for a specific user
- **THEN** 2FA is deactivated for that user and all recovery codes are invalidated

### Requirement: TOTP codes are time-based and have a validity window

The system SHALL accept TOTP codes that are valid within a 30-second window. The system SHALL tolerate a ±1 window (60 seconds total) to account for clock skew between the server and the user's device.

#### Scenario: Current TOTP code is accepted
- **WHEN** a user provides a TOTP code matching the current time window
- **THEN** the code is accepted

#### Scenario: Adjacent TOTP code is accepted
- **WHEN** a user provides a TOTP code matching the previous or next time window
- **THEN** the code is accepted

#### Scenario: Expired TOTP code is rejected
- **WHEN** a user provides a TOTP code outside the ±1 window
- **THEN** the code is rejected with an appropriate error

### Requirement: TOTP verification is rate-limited

The system SHALL limit TOTP verification attempts to prevent brute-force attacks. After 5 consecutive failed attempts within 5 minutes, the system SHALL temporarily lock TOTP verification for that user for 5 minutes.

#### Scenario: Rate limit not yet reached
- **WHEN** a user provides an invalid TOTP code and has fewer than 5 consecutive failures
- **THEN** the attempt is counted and the user can try again

#### Scenario: Rate limit exceeded
- **WHEN** a user provides an invalid TOTP code and has reached 5 consecutive failures
- **THEN** TOTP verification is locked for 5 minutes and the user receives a rate-limit error

### Requirement: 2FA status is exposed in the user profile

The system SHALL indicate whether 2FA is enabled in the user profile response so the frontend can display the current status and offer appropriate actions.

#### Scenario: User with 2FA enabled checks profile
- **WHEN** a user with 2FA enabled requests their profile
- **THEN** the profile response includes `twoFactorEnabled: true`

#### Scenario: User without 2FA enabled checks profile
- **WHEN** a user without 2FA enabled requests their profile
- **THEN** the profile response includes `twoFactorEnabled: false`

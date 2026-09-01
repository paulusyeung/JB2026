## Backend

### User Model & Database

- [x] Add `TwoFactorEnabled`, `TwoFactorSecret`, `TwoFactorRecoveryCodes` fields to `LegacyIdentityUser`
- [x] Update `ILegacyIdentityService` with methods: `GetTwoFactorStatus(userId)`, `EnableTwoFactor(userId, secret, recoveryCodes)`, `DisableTwoFactor(userId)`, `ValidateTwoFactorCode(userId, code)`, `UseRecoveryCode(userId, code)`
- [x] Update `HybridLegacyIdentityService` to read 2FA data from `UserInfo.MetadataXml` (query `UserInfo` table directly for 2FA status, not just the view)
- [x] Create helper to parse/write 2FA XML elements in `MetadataXml` (same pattern as `ExtractEmailFromMetadata` / `SetEmailInMetadata` in AdminController)
- [x] Handle config-based users: add optional `TwoFactor` section to `LegacyIdentityOptions` config (or document that config users cannot use 2FA)

### TOTP Service

- [x] Create `ITwoFactorService` interface with methods: `GenerateSecret()`, `GetProvisioningUri(userId, secret)`, `ValidateCode(secret, code)`, `HashRecoveryCodes(codes)`, `VerifyRecoveryCode(hashedCodes, inputCode)`
- [x] Create `TwoFactorService` implementation using `OtpNet` for TOTP operations
- [x] Implement AES-256 encryption for TOTP secrets when writing to `MetadataXml` (encryption key from `Encryption:Key` config)
- [x] Implement salted SHA-256 hashing for recovery codes when writing to `MetadataXml`

### Auth Controller

- [x] Add `TwoFactorToken` model (temporary JWT with `purpose: "2fa"`, 5-minute expiry)
- [x] Modify `CreateTokenInternalAsync` to check `TwoFactorEnabled` after password validation — if enabled, return temporary token instead of access token
- [x] Add `POST /api/v2/auth/2fa/verify` endpoint — accepts temporary token + TOTP/recovery code, returns full TokenResponse
- [x] Add `POST /api/v2/auth/2fa/setup` endpoint (authenticated) — generates secret + provisioning URI
- [x] Add `POST /api/v2/auth/2fa/confirm` endpoint (authenticated) — verifies TOTP code, activates 2FA, returns recovery codes
- [x] Add `POST /api/v2/auth/2fa/disable` endpoint (authenticated) — requires password + TOTP/recovery code to disable
- [x] Add `DELETE /api/v2/auth/2fa` endpoint (admin) — disables 2FA for any user

### Rate Limiting

- [x] Implement per-user TOTP attempt tracking with in-memory `ConcurrentDictionary`
- [x] Lock TOTP verification for 5 minutes after 5 consecutive failures
- [x] Reset failure count on successful verification

## Frontend

### Types & Services

- [x] Add `TwoFactorSetupResponse`, `TwoFactorVerifyResponse`, `TwoFactorStatusResponse` to `types/api.ts`
- [x] Update `TokenResponse` to include optional `requires2fa` and `twoFactorToken` fields
- [x] Add 2FA API functions to `services/auth.ts`: `setupTwoFactor()`, `confirmTwoFactor(code)`, `disableTwoFactor(password, code)`, `verifyTwoFactor(tempToken, code)`, `getTwoFactorStatus()`

### Login Flow

- [x] Update `session.ts` store to handle two-step login — store temporary token in memory (not localStorage)
- [x] Update `LoginView.vue` to detect `requires2fa` response and show TOTP input screen
- [x] Add TOTP input component (6-digit code, auto-submit on completion)
- [x] Handle temporary token expiry (return to username/password form)
- [x] Handle rate limit errors during 2FA verification

### 2FA Management UI

- [x] Add 2FA section to `StaffMemberRecordDialog.vue` — shows status, enable/disable buttons (in the existing profile dialog, accessed from topbar)
- [x] Create 2FA enrollment sub-dialog — shows QR code, recovery codes, confirmation input
- [x] Create 2FA disable sub-dialog — requires password + TOTP code to disable

### Navigation & Guards

- [x] Add route for `/app/login/2fa` (unauthenticated, only accessible with temporary token)

## Shared

- [x] Add i18n strings for 2FA flows (en, zh-TW, zh-CN)
- [x] Add `OtpNet` NuGet package to `JB2026.Api`
- [x] Add `qrcode` npm package to `ClientApp`
- [x] Add `Encryption:Key` configuration to `appsettings.json` and `appsettings.Development.json`

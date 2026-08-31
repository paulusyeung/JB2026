## Backend

### User Model & Database

- [ ] Add `TwoFactorEnabled`, `TwoFactorSecret`, `TwoFactorRecoveryCodes` fields to `LegacyIdentityUser`
- [ ] Update `ILegacyIdentityService` with methods: `GetTwoFactorStatus(userId)`, `EnableTwoFactor(userId, secret, recoveryCodes)`, `DisableTwoFactor(userId)`, `ValidateTwoFactorCode(userId, code)`, `UseRecoveryCode(userId, code)`
- [ ] Update `HybridLegacyIdentityService` to read 2FA data from `UserInfo.MetadataXml` (query `UserInfo` table directly for 2FA status, not just the view)
- [ ] Create helper to parse/write 2FA XML elements in `MetadataXml` (same pattern as `ExtractEmailFromMetadata` / `SetEmailInMetadata` in AdminController)
- [ ] Handle config-based users: add optional `TwoFactor` section to `LegacyIdentityOptions` config (or document that config users cannot use 2FA)

### TOTP Service

- [ ] Create `ITwoFactorService` interface with methods: `GenerateSecret()`, `GetProvisioningUri(userId, secret)`, `ValidateCode(secret, code)`, `HashRecoveryCodes(codes)`, `VerifyRecoveryCode(hashedCodes, inputCode)`
- [ ] Create `TwoFactorService` implementation using `OtpNet` for TOTP operations
- [ ] Implement AES-256 encryption for TOTP secrets when writing to `MetadataXml` (encryption key from `Encryption:Key` config)
- [ ] Implement salted SHA-256 hashing for recovery codes when writing to `MetadataXml`

### Auth Controller

- [ ] Add `TwoFactorToken` model (temporary JWT with `purpose: "2fa"`, 5-minute expiry)
- [ ] Modify `CreateTokenInternalAsync` to check `TwoFactorEnabled` after password validation — if enabled, return temporary token instead of access token
- [ ] Add `POST /api/v2/auth/2fa/verify` endpoint — accepts temporary token + TOTP/recovery code, returns full TokenResponse
- [ ] Add `POST /api/v2/auth/2fa/setup` endpoint (authenticated) — generates secret + provisioning URI
- [ ] Add `POST /api/v2/auth/2fa/confirm` endpoint (authenticated) — verifies TOTP code, activates 2FA, returns recovery codes
- [ ] Add `POST /api/v2/auth/2fa/disable` endpoint (authenticated) — requires password + TOTP/recovery code to disable
- [ ] Add `DELETE /api/v2/auth/2fa` endpoint (admin) — disables 2FA for any user

### Rate Limiting

- [ ] Implement per-user TOTP attempt tracking with in-memory `ConcurrentDictionary`
- [ ] Lock TOTP verification for 5 minutes after 5 consecutive failures
- [ ] Reset failure count on successful verification

## Frontend

### Types & Services

- [ ] Add `TwoFactorSetupResponse`, `TwoFactorVerifyResponse`, `TwoFactorStatusResponse` to `types/api.ts`
- [ ] Update `TokenResponse` to include optional `requires2fa` and `twoFactorToken` fields
- [ ] Add 2FA API functions to `services/auth.ts`: `setupTwoFactor()`, `confirmTwoFactor(code)`, `disableTwoFactor(password, code)`, `verifyTwoFactor(tempToken, code)`, `getTwoFactorStatus()`

### Login Flow

- [ ] Update `session.ts` store to handle two-step login — store temporary token in memory (not localStorage)
- [ ] Update `LoginView.vue` to detect `requires2fa` response and show TOTP input screen
- [ ] Add TOTP input component (6-digit code, auto-submit on completion)
- [ ] Handle temporary token expiry (return to username/password form)
- [ ] Handle rate limit errors during 2FA verification

### 2FA Management UI

- [ ] Add 2FA section to `StaffMemberRecordDialog.vue` — shows status, enable/disable buttons (in the existing profile dialog, accessed from topbar)
- [ ] Create 2FA enrollment sub-dialog — shows QR code, recovery codes, confirmation input
- [ ] Create 2FA disable sub-dialog — requires password + TOTP code to disable

### Navigation & Guards

- [ ] Add route for `/app/login/2fa` (unauthenticated, only accessible with temporary token)

## Shared

- [ ] Add i18n strings for 2FA flows (en, zh-TW, zh-CN)
- [ ] Add `OtpNet` NuGet package to `JB2026.Api`
- [ ] Add `qrcode` npm package to `ClientApp`
- [ ] Add `Encryption:Key` configuration to `appsettings.json` and `appsettings.Development.json`

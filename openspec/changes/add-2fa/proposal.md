## Why

Single-factor authentication is a critical security gap. A compromised password grants full access to the job management system — quotations, billing, CRM, stock, SML invoicing. Adding TOTP-based two-factor authentication (compatible with any standard authenticator app — Google Authenticator, Authy, Microsoft Authenticator, 1Password, Bitwarden, etc.) provides a second layer of defense without requiring external identity providers. This is especially important since the system is being prepared for open-source release and will be exposed to broader scrutiny.

## What Changes

- **New TOTP enrollment flow (opt-in)**: Users can choose to enable 2FA from their profile settings, which generates a secret and presents a QR code for scanning with an authenticator app. 2FA is entirely optional — users who do not enable it continue with password-only login unchanged
- **Modified login flow**: After password verification, 2FA-enabled users are prompted for a 6-digit TOTP code before receiving a JWT
- **Recovery codes**: A set of one-time recovery codes is generated at enrollment, allowing access if the authenticator device is lost
- **Admin recovery**: Admins can disable 2FA for a user after identity verification (existing admin user management panel)
- **Backend TOTP service**: New service for generating secrets, verifying TOTP codes, and managing recovery codes
- **User model extension**: `TwoFactorEnabled`, `TwoFactorSecret`, `TwoFactorRecoveryCodes` fields added to the identity model

## Capabilities

### New Capabilities

- `auth/two-factor`: TOTP-based two-factor authentication — enrollment, verification, recovery codes, and admin disable

### Modified Capabilities

- `auth/login`: Login response now includes `requires2fa` flag and supports two-step authentication (password → TOTP)

## Impact

- **Backend**: `AuthController.cs` (login flow), new `TwoFactorService`, `HybridLegacyIdentityService` (user model), DB schema migration for 2FA fields
- **Frontend**: `StaffMemberRecordDialog.vue` (2FA section in existing profile dialog), `LoginView.vue` (two-step login), `session.ts` (two-step state)
- **Dependencies**: New NuGet package (`OtpNet` or equivalent), new npm package (`qrcode` for QR generation on frontend, or generate URI server-side)
- **Database**: Schema migration to add 2FA columns to the user table
- **Security**: TOTP secrets must be encrypted at rest; recovery codes must be hashed

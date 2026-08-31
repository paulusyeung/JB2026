## Context

JB2026 currently uses single-factor JWT bearer authentication. The `AuthController` issues tokens after a single username/password check via `HybridLegacyIdentityService`. Passwords are stored in plaintext (both in config and in the legacy `vwUserList_Active` view). The frontend login is a single form in `LoginView.vue` that calls `session.login()` which issues a single API call.

Adding 2FA requires a two-step login flow, a new TOTP service, and changes to the user model and frontend login flow.

## Goals / Non-Goals

**Goals:**
- Add TOTP-based 2FA (compatible with any standard authenticator app) to the existing JWT auth system — **entirely opt-in**, no user is forced to enable it
- Support enrollment, verification, recovery codes, and admin disable
- Zero disruption to existing login flow for users who don't enable 2FA (single-step password login remains unchanged)

**Non-Goals:**
- Password hashing (addressed separately — plaintext passwords are a prerequisite fix but not part of this change)
- WebAuthn/FIDO2 hardware key support
- SMS-based 2FA
- User self-registration (users are managed via admin panel)

## Decisions

### 1. Use `OtpNet` library for TOTP on the backend

**Decision**: Use the `OtpNet` NuGet package for TOTP secret generation and verification.

**Why**: `OtpNet` is a well-maintained, widely-used .NET library for HOTP/TOTP. It handles RFC 6238 compliance, time windowing, and secret generation. Alternatives like `GoogleAuthenticator` exist but are wrappers around similar logic with less flexibility.

**Alternatives considered**:
- `GoogleAuthenticator` — simpler API but less configurable
- Rolling our own — unnecessary complexity, security risk

### 2. Use `qrcode.js` on the frontend for QR generation

**Decision**: Generate the `otpauth://` URI server-side and render the QR code on the frontend using the `qrcode` npm package.

**Why**: QR generation in the browser avoids server-side image dependencies. The `otpauth://` URI format is standard and compatible with all major authenticator apps.

**Alternatives considered**:
- Server-side QR generation (returns image) — adds API complexity, harder to cache
- Inline SVG QR codes — heavier library, no benefit over `qrcode`

### 3. Store 2FA data in `UserInfo.MetadataXml` (XML column)

**Decision**: Store 2FA state (enabled flag, encrypted secret, hashed recovery codes) inside the existing `UserInfo.MetadataXml` XML column. The structure follows the established pattern used by RBAC and email:

```xml
<Metadata>
  <Email>user@example.com</Email>
  <TwoFactor>
    <Enabled>true</Enabled>
    <Secret>encrypted-base32-secret</Secret>
    <RecoveryCodes>hash1|salt1,hash2|salt2,...</RecoveryCodes>
  </TwoFactor>
</Metadata>
```

**Why**: No schema migration needed. The `MetadataXml` column already exists and is used for RBAC and email. Consistent with the project's metadata pattern.

**Alternatives considered**:
- New `UserTwoFactor` table — cleaner separation but adds a table and requires joins
- Add columns to `UserInfo` — touches legacy schema, requires view updates

**Important**: `HybridLegacyIdentityService` reads from the read-only view `vwUserList_Active`, which does NOT include `MetadataXml`. For2FA checks during login, the service must also query the `UserInfo` table directly (via `JB5LegacyWriteContext` or a new read query). This is the same pattern used by `AdminController` (line 84: joins view + table).

### 4. Hash recovery codes with salted SHA-256

**Decision**: Generate random recovery codes, hash them with a per-code salt using SHA-256, and store the hashes.

**Why**: Recovery codes are one-time use. Hashing prevents an attacker with DB read access from using them. The verification flow hashes the input code and compares against stored hashes.

**Alternatives considered**:
- Plaintext storage — allows DB-level reuse, unacceptable
- Bcrypt — overkill for random high-entropy codes, SHA-256 with salt is sufficient

### 5. Use a temporary token (short-lived JWT) for the two-step login flow

**Decision**: After successful password verification, issue a temporary JWT (5-minute expiry, `purpose: "2fa"`) that the frontend sends back with the TOTP code. This temporary token is NOT a valid access token.

**Why**: Avoids storing server-side session state for pending 2FA flows. The temporary token carries the user identity and is verified by signature, not by database lookup.

**Alternatives considered**:
- Server-side session store — adds state management complexity
- Encrypting the full user payload in the response — adds latency and complexity

### 6. Rate-limit TOTP verification per-user (in-memory)

**Decision**: Track failed TOTP attempts per user in-memory using a `ConcurrentDictionary`. Lock for 5 minutes after 5 consecutive failures.

**Why**: Prevents brute-force attacks on the 6-digit code. In-memory is sufficient for a single-server deployment. For multi-server, this would need Redis (deferred).

**Alternatives considered**:
- Database-backed rate limiting — adds latency and DB load
- Global rate limiting — too coarse, blocks all users

## Risks / Trade-offs

**[Risk] Plaintext passwords undermine 2FA value** → Mitigated by: flagging this as a prerequisite fix (password hashing). The 2FA spec is designed to work with hashed passwords; the plaintext issue is a separate change.

**[Risk] In-memory rate limiting doesn't work across server instances** → Mitigated by: the system currently runs as a single-instance deployment. Documented as a known limitation for future multi-server scaling.

**[Risk] TOTP secret encryption key rotation** → Mitigated by: storing a key version alongside the encrypted secret. If the key rotates, old secrets can still be decrypted with the previous key. Key rotation is deferred to a future change.

**[Risk] User lockout if authenticator device is lost and no recovery codes** → Mitigated by: admin disable capability. Admins can disable 2FA for any user after identity verification.

**[Risk] Config-based users cannot use 2FA** → Mitigated by: config-based users (defined in `LegacyIdentity` config section) have no database row, so MetadataXml storage doesn't apply. If needed, add a `TwoFactor` section to the config options. For initial release, 2FA is only available to database users.

## Migration Plan

1. Deploy backend with 2FA endpoints (backward compatible — existing login flow unchanged). No schema migration needed — 2FA data goes into existing `MetadataXml` column.
2. Deploy frontend with 2FA support (backward compatible — single-step login unchanged)
3. Users opt into 2FA at their own pace via profile dialog (topbar → click name)

**Rollback**: Remove 2FA endpoints, revert frontend changes. Existing `MetadataXml` entries with `<TwoFactor>` elements are harmlessly ignored. Users without 2FA are unaffected.

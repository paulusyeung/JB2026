## Why

The current authentication system uses short-lived JWT access tokens (60 minutes) with no refresh token mechanism. When users check "Keep Me Signed In," the flag is ignored—the session still expires after the token lifetime. This forces users to re-authenticate frequently, degrading the experience for trusted devices.

## What Changes

- **Add Refresh Token Support**: Introduce a persistent refresh token (default 30 days) that allows silent access token renewal without re-authentication.
- **Wire Up "Keep Me Signed In"**: The login checkbox will now control whether a refresh token is issued and stored.
- **Add Token Refresh Endpoint**: New `POST /api/v2/auth/refresh` endpoint to exchange a refresh token for a new access token.
- **Add Token Revoke Endpoint**: New `POST /api/v2/auth/revoke` endpoint for explicit logout and token invalidation.
- **Frontend Auto-Refresh**: Implement an Axios interceptor that automatically refreshes the access token when it expires using the stored refresh token.
- **Breakdown Token Request**: The `TokenRequest` model gains an optional `keepMeSignedIn` boolean field. `TokenResponse` now includes an optional `refreshToken` field (additive and backward-compatible for tolerant clients).

## API Contract Summary

- **Refresh Endpoint**: `POST /api/v2/auth/refresh`
  - Request body: `{ "refreshToken": "<opaque-token>" }`
  - Success response (200): `{ "accessToken": "<jwt>", "refreshToken": "<opaque-token>" }`
  - Failure response (401): `{ "error": "invalid_refresh_token" }` for expired/invalid/used tokens
- **Revoke Endpoint**: `POST /api/v2/auth/revoke`
  - Request body: `{ "refreshToken": "<opaque-token>" }`
  - Success response (204): no body, idempotent for unknown/already-invalid tokens

## Capabilities

### New Capabilities
- `refresh-token-management`: Server-side refresh token issuance, storage, validation, rotation, and revocation.
- `auto-token-refresh`: Frontend Axios interceptor that detects 401 responses and silently refreshes the access token using the stored refresh token.

### Modified Capabilities
- `authentication`: The login flow now optionally issues refresh tokens. The session store now manages both access and refresh tokens. Token expiration behavior changes based on the "Keep Me Signed In" preference.

## Impact

- **Backend**:
  - `JB2026.Api/Controllers/AuthController.cs`: New refresh and revoke endpoints.
  - `JB2026.Api/Services/JwtTokenService.cs`: Modified to accept `keepMeSignedIn` flag and potentially issue longer-lived tokens.
  - `JB2026.Api/Services/IJwtTokenService.cs`: Interface update.
  - `JB2026.Api/Models/TokenRequest.cs`: New `KeepMeSignedIn` property.
  - `JB2026.Api/Models/TokenResponse.cs`: New `RefreshToken` property.
  - New `IRefreshTokenService` and implementation for token storage (in-memory singleton for v1, database-backed later).
  - `appsettings.json`: New `Jwt:RefreshTokenExpiryDays` configuration (default 30).
- **Frontend**:
  - `ClientApp/src/services/auth.ts`: New `refreshToken` and `revokeToken` functions.
  - `ClientApp/src/stores/session.ts`: Manage refresh token storage, auto-refresh logic.
  - `ClientApp/src/views/LoginView.vue`: Pass `keepMeSignedIn` to the login service.
  - `ClientApp/src/services/api.ts`: Axios interceptor for automatic token refresh on 401.
  - `ClientApp/src/types/api.ts`: Updated `TokenResponse` type with `refreshToken`.
- **Security**: Refresh tokens will be rotated on each use (one-time use pattern) to detect token theft.
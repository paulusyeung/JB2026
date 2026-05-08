## Context

The current authentication flow issues a short-lived JWT access token (60 minutes) stored in `localStorage`. The "Keep Me Signed In" checkbox on the login page is currently ignored—no refresh token is issued, and the session expires when the access token does. Users on trusted devices must re-authenticate frequently.

**Current State:**
- `JwtTokenService.CreateToken()` always issues a 60-minute token.
- `AuthController` accepts only `username` and `password`.
- Frontend `session.ts` stores only the access token in `localStorage`.
- No mechanism exists to silently renew tokens.

**Constraints:**
- Must maintain backward compatibility with existing clients that don't send `keepMeSignedIn`.
- Refresh tokens must be revocable (e.g., on logout).
- Token theft should be detectable via rotation.

## Goals / Non-Goals

**Goals:**
- Issue a long-lived refresh token (configurable, default 30 days) when `keepMeSignedIn` is true.
- Provide an endpoint to exchange a refresh token for a new access token.
- Provide an endpoint to revoke refresh tokens (logout).
- Implement automatic access token refresh on the frontend via Axios interceptor.
- Rotate refresh tokens on each use to detect unauthorized usage.

**Non-Goals:**
- Database-backed refresh token persistence (v1 uses an in-memory dictionary; can be upgraded later).
- Multi-device management UI (listing active sessions).
- Sliding expiration for access tokens beyond the refresh cycle.

## Decisions

### 1. Refresh Token Storage: In-Memory Dictionary (v1)

**Decision:** Use a `ConcurrentDictionary<string, RefreshTokenRecord>` in a singleton `RefreshTokenService` for v1.

**Rationale:**
- Simpler to implement and test no database schema changes are needed.
- Sufficient for a single-server deployment.
- Can be swapped for a database-backed implementation later by changing the `IRefreshTokenService` implementation.

**Alternatives Considered:**
- **Database-backed**: More robust for multi-server deployments but requires migration and EF Core setup. Deferred to v2.
- **Redis**: Good for distributed scenarios but adds infrastructure dependency. Deferred to v2.

### 1.1 API Contract Shape (Refresh/Revoke)

**Decision:** Define explicit request/response DTOs for refresh/revoke APIs.

**Contract:**
- `POST /api/v2/auth/refresh`
   - Request: `{ refreshToken: string }`
   - Success (200): `{ accessToken: string, refreshToken: string }`
   - Failure (401): `{ error: "invalid_refresh_token" }`
- `POST /api/v2/auth/revoke`
   - Request: `{ refreshToken: string }`
   - Success (204): no content
   - Unknown/invalid token: also 204 (idempotent)

### 2. Refresh Token Rotation

**Decision:** Each refresh token is single-use. When exchanged, the old token is invalidated and a new one is issued.

**Rationale:**
- Detects token theft: if two requests use the same refresh token, the second will fail, indicating the token was compromised.
- Industry best practice (OWASP recommendation).

**Scope Clarification:** Reuse detection revokes all active refresh tokens for the affected user in v1.

### 3. Configuration via `appsettings.json`

**Decision:** Add `Jwt:RefreshTokenExpiryDays` (default 30) to control refresh token lifetime.

**Rationale:**
- Keeps the existing pattern of JWT configuration in one place.
- Allows easy tuning without code changes.

### 4. Frontend: Axios Interceptor for Auto-Refresh

**Decision:** Use an Axios response interceptor to catch 401 responses and attempt a refresh before rejecting the promise.

**Rationale:**
- Transparent to the rest of the application.
- Prevents multiple concurrent refresh attempts by queuing requests during refresh.
- Reuses existing `apiClient` infrastructure.

### 5. Refresh Token Format

**Decision:** Refresh tokens will be random 64-byte strings (Base64-encoded), not JWTs.

**Rationale:**
- Refresh tokens contain no claims—they are opaque identifiers looked up in the store.
- Simpler and more secure than embedding data in the token itself.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| In-memory store loses tokens on server restart | All "Keep Me Signed In" sessions will be invalidated. Acceptable for v1; database-backed store planned for v2. |
| Concurrent requests during refresh may fail | Axios interceptor will queue pending requests and retry them after refresh completes. |
| Token theft before rotation detects it | There's a small window between token issuance and first use. Mitigated by short access token lifetime (60 min). |
| Backward compatibility with legacy clients | `keepMeSignedIn` defaults to `false`; legacy clients continue to work unchanged. |

## Security Mitigations for localStorage Refresh Tokens

- Do not log tokens in API or frontend logs (including error telemetry).
- Enforce strict CSP and avoid unsafe inline scripts in the frontend.
- Rotate refresh tokens on every successful refresh and revoke all user tokens on reuse detection.
- Keep access token lifetime short (60 minutes) and require full login when refresh fails.

## Migration Plan

1. **Backend Changes:**
   - Add `RefreshTokenService` and register it in DI.
   - Update `TokenRequest` and `TokenResponse` models.
   - Add request/response DTOs for refresh/revoke contracts.
   - Add `/api/v2/auth/refresh` and `/api/v2/auth/revoke` endpoints.
   - Update `JwtTokenService` to accept `keepMeSignedIn` parameter.

2. **Frontend Changes:**
   - Update `signIn` service to send `keepMeSignedIn`.
   - Update `session.ts` to store and manage refresh tokens.
   - Add Axios interceptor for auto-refresh.
   - Update `LoginView.vue` to pass the flag.

3. **Rollback Strategy:**
   - If issues arise, revert the frontend changes. Backend endpoints are additive and won't break existing clients.
   - Feature flag the interceptor if needed.

## Open Questions

- Should we support "revoke all tokens for a user" (e.g., on password change)? **Decision:** Out of scope for admin-triggered flows in v1, but reuse detection-triggered revoke-all is in scope.
- Should the refresh token be HTTP-only cookie or stored in `localStorage`? **Decision:** `localStorage` for simplicity and consistency with current pattern. HTTP-only cookies would require backend session management, which is a larger change.

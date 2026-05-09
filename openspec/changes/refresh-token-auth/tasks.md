## 1. Backend: Refresh Token Infrastructure

- [x] 1.1 Create `RefreshTokenRecord` model with properties: `Token`, `UserId`, `ExpiresAtUtc`, `IsUsed`, `CreatedAtUtc`
- [x] 1.2 Create `IRefreshTokenService` interface with methods: `CreateAsync`, `ValidateAsync`, `RevokeAsync`, `RevokeAllForUserAsync`
- [x] 1.3 Implement `RefreshTokenService` with in-memory `ConcurrentDictionary` storage
- [x] 1.4 Register `RefreshTokenService` as singleton in DI container (`Program.cs`)
- [x] 1.5 Add `Jwt:RefreshTokenExpiryDays` configuration key to `appsettings.json` (default: 30)

## 2. Backend: Update Auth Models and Service

- [x] 2.1 Add optional `KeepMeSignedIn` boolean property to `TokenRequest` model
- [x] 2.2 Add optional `RefreshToken` string property to `TokenResponse` model
- [x] 2.3 Update `IJwtTokenService.CreateToken` signature to accept optional `keepMeSignedIn` parameter
- [x] 2.4 Update `JwtTokenService.CreateToken` to pass `keepMeSignedIn` through

## 3. Backend: Auth Controller Endpoints

- [x] 3.1 Update `POST /api/v2/auth/token` to accept `keepMeSignedIn` and issue refresh token when true
- [x] 3.2 Implement `POST /api/v2/auth/refresh` endpoint for token exchange with rotation
- [x] 3.3 Implement `POST /api/v2/auth/revoke` endpoint for explicit token revocation
- [x] 3.4 Add explicit request/response DTOs for refresh/revoke contracts and error payloads
- [x] 3.5 Add unit tests for refresh token issuance, exchange, rotation, and revocation

## 4. Frontend: Update Types and Services

- [x] 4.1 Update `TokenResponse` TypeScript interface to include optional `refreshToken` field
- [x] 4.2 Update `signIn` function in `auth.ts` to accept and send `keepMeSignedIn` parameter
- [x] 4.3 Add `refreshToken` function in `auth.ts` to call `/api/v2/auth/refresh`
- [x] 4.4 Add `revokeToken` function in `auth.ts` to call `/api/v2/auth/revoke`

## 5. Frontend: Session Store Updates

- [x] 5.1 Add `refreshToken` state property to session store
- [x] 5.2 Update `login` action to store refresh token in `localStorage` when `keepMeSignedIn` is true
- [x] 5.3 Update `logout` action to revoke refresh token via API and clear from `localStorage`
- [x] 5.4 Update `initialize` action to restore refresh token from `localStorage` on app load

## 6. Frontend: Axios Interceptor for Auto-Refresh

- [x] 6.1 Add response interceptor to `apiClient` to catch 401 responses
- [x] 6.2 Implement refresh logic: call `/api/v2/auth/refresh`, update stored tokens
- [x] 6.3 Implement request queuing: pause concurrent requests during refresh
- [x] 6.4 Retry failed requests with new access token after successful refresh
- [x] 6.5 Handle refresh failure: clear session and redirect to login

## 7. Frontend: Login View Updates

- [x] 7.1 Update `LoginView.vue` to pass `keepMeSignedIn` value to `session.login()`
- [x] 7.2 Ensure checkbox state is properly bound and passed through the login flow

## 8. Testing and Validation

- [x] 8.1 Test login with `keepMeSignedIn: true` verifies refresh token is issued and stored
- [x] 8.2 Test login with `keepMeSignedIn: false` verifies no refresh token is issued
- [x] 8.3 Test auto-refresh on 401: request succeeds after token renewal
- [x] 8.4 Test concurrent requests during refresh: all succeed after single refresh
- [x] 8.5 Test logout: refresh token is revoked and cleared from storage
- [x] 8.6 Test expired refresh token: user is redirected to login
- [x] 8.7 Test token theft detection: reused refresh token revokes all user tokens
- [x] 8.8 Test revoke idempotency: unknown/already-invalid refresh token returns HTTP 204

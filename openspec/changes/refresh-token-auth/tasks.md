## 1. Backend: Refresh Token Infrastructure

- [ ] 1.1 Create `RefreshTokenRecord` model with properties: `Token`, `UserId`, `ExpiresAtUtc`, `IsUsed`, `CreatedAtUtc`
- [ ] 1.2 Create `IRefreshTokenService` interface with methods: `CreateAsync`, `ValidateAsync`, `RevokeAsync`, `RevokeAllForUserAsync`
- [ ] 1.3 Implement `RefreshTokenService` with in-memory `ConcurrentDictionary` storage
- [ ] 1.4 Register `RefreshTokenService` as singleton in DI container (`Program.cs`)
- [ ] 1.5 Add `Jwt:RefreshTokenExpiryDays` configuration key to `appsettings.json` (default: 30)

## 2. Backend: Update Auth Models and Service

- [ ] 2.1 Add optional `KeepMeSignedIn` boolean property to `TokenRequest` model
- [ ] 2.2 Add optional `RefreshToken` string property to `TokenResponse` model
- [ ] 2.3 Update `IJwtTokenService.CreateToken` signature to accept optional `keepMeSignedIn` parameter
- [ ] 2.4 Update `JwtTokenService.CreateToken` to pass `keepMeSignedIn` through

## 3. Backend: Auth Controller Endpoints

- [ ] 3.1 Update `POST /api/v2/auth/token` to accept `keepMeSignedIn` and issue refresh token when true
- [ ] 3.2 Implement `POST /api/v2/auth/refresh` endpoint for token exchange with rotation
- [ ] 3.3 Implement `POST /api/v2/auth/revoke` endpoint for explicit token revocation
- [ ] 3.4 Add explicit request/response DTOs for refresh/revoke contracts and error payloads
- [ ] 3.5 Add unit tests for refresh token issuance, exchange, rotation, and revocation

## 4. Frontend: Update Types and Services

- [ ] 4.1 Update `TokenResponse` TypeScript interface to include optional `refreshToken` field
- [ ] 4.2 Update `signIn` function in `auth.ts` to accept and send `keepMeSignedIn` parameter
- [ ] 4.3 Add `refreshToken` function in `auth.ts` to call `/api/v2/auth/refresh`
- [ ] 4.4 Add `revokeToken` function in `auth.ts` to call `/api/v2/auth/revoke`

## 5. Frontend: Session Store Updates

- [ ] 5.1 Add `refreshToken` state property to session store
- [ ] 5.2 Update `login` action to store refresh token in `localStorage` when `keepMeSignedIn` is true
- [ ] 5.3 Update `logout` action to revoke refresh token via API and clear from `localStorage`
- [ ] 5.4 Update `initialize` action to restore refresh token from `localStorage` on app load

## 6. Frontend: Axios Interceptor for Auto-Refresh

- [ ] 6.1 Add response interceptor to `apiClient` to catch 401 responses
- [ ] 6.2 Implement refresh logic: call `/api/v2/auth/refresh`, update stored tokens
- [ ] 6.3 Implement request queuing: pause concurrent requests during refresh
- [ ] 6.4 Retry failed requests with new access token after successful refresh
- [ ] 6.5 Handle refresh failure: clear session and redirect to login

## 7. Frontend: Login View Updates

- [ ] 7.1 Update `LoginView.vue` to pass `keepMeSignedIn` value to `session.login()`
- [ ] 7.2 Ensure checkbox state is properly bound and passed through the login flow

## 8. Testing and Validation

- [ ] 8.1 Test login with `keepMeSignedIn: true` verifies refresh token is issued and stored
- [ ] 8.2 Test login with `keepMeSignedIn: false` verifies no refresh token is issued
- [ ] 8.3 Test auto-refresh on 401: request succeeds after token renewal
- [ ] 8.4 Test concurrent requests during refresh: all succeed after single refresh
- [ ] 8.5 Test logout: refresh token is revoked and cleared from storage
- [ ] 8.6 Test expired refresh token: user is redirected to login
- [ ] 8.7 Test token theft detection: reused refresh token revokes all user tokens
- [ ] 8.8 Test revoke idempotency: unknown/already-invalid refresh token returns HTTP 204

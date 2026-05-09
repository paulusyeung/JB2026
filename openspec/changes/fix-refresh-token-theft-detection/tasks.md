## 1. Backend: Add ValidateAndConsumeAsync to Interface

- [x] 1.1 Add `ValidateAndConsumeAsync(string refreshToken)` method to `IRefreshTokenService` interface with XML documentation

## 2. Backend: Implement ValidateAndConsumeAsync

- [x] 2.1 Implement `ValidateAndConsumeAsync` in `RefreshTokenService` using `ConcurrentDictionary.TryRemove` for atomic validation and consumption
- [x] 2.2 Ensure expired tokens return null (check `ExpiresAtUtc` after successful remove)
- [x] 2.3 Ensure already-consumed tokens return null (TryRemove fails → return null)

## 3. Backend: Update AuthController

- [x] 3.1 Replace `ValidateAsync` + `RevokeAsync` sequence in `RefreshToken` endpoint with single `ValidateAndConsumeAsync` call
- [x] 3.2 Remove the now-unnecessary `RevokeAsync` call after validation
- [x] 3.3 Update comment to reflect atomic operation

## 4. Tests: Update Unit Tests

- [x] 4.1 Add test: `ValidateAndConsumeAsync_ValidTokenReturnsUserIdAndRemovesToken`
- [x] 4.2 Add test: `ValidateAndConsumeAsync_AlreadyConsumedTokenReturnsNull`
- [x] 4.3 Add test: `ValidateAndConsumeAsync_ExpiredTokenReturnsNull`
- [x] 4.4 Add test: `ValidateAndConsumeAsync_ConcurrentCallsOnlyOneSucceeds`
- [x] 4.5 Update existing theft detection test to use `ValidateAndConsumeAsync` instead of `ValidateAsync`

## 5. Verification

- [x] 5.1 Run all unit tests to ensure no regressions
- [x] 5.2 Verify refresh flow works end-to-end with keepMeSignedIn: true
- [x] 5.3 Verify reused refresh token returns 401


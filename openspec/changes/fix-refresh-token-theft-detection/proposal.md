## Why

The refresh token theft detection mechanism in `RefreshTokenService.ValidateAsync` is broken—`IsUsed` is checked but never set to `true`, so reused tokens are not detected as theft. Additionally, the validate-then-revoke flow has a race condition where two concurrent requests with the same token can both pass validation before either revokes it. This means token theft cannot be reliably detected, violating the security goal established in the original `refresh-token-auth` change.

## What Changes

- **Add `ValidateAndConsumeAsync` method**: A new atomic method on `IRefreshTokenService` that validates and removes the token in a single `ConcurrentDictionary.TryRemove` call, eliminating the race window.
- **Update `AuthController.RefreshToken`**: Replace the two-step `ValidateAsync` + `RevokeAsync` sequence with a single `ValidateAndConsumeAsync` call.
- **Update unit tests**: Fix the theft detection test to use the new method and verify correct behavior.
- **Keep `ValidateAsync` for non-consuming checks**: Retain for scenarios where validation is needed without consumption (e.g., admin inspection), but the refresh flow will no longer use it.

## Capabilities

### New Capabilities
- None

### Modified Capabilities
- `refresh-token-management`: The requirement for token theft detection now specifies atomic validate-and-consume behavior. The existing `ValidateAsync` check-then-revoke pattern is replaced by a single atomic operation to prevent race conditions.

## Impact

- **Backend**:
  - `JB2026.Api/Services/IRefreshTokenService.cs`: New `ValidateAndConsumeAsync` method added to interface.
  - `JB2026.Api/Services/RefreshTokenService.cs`: Implement `ValidateAndConsumeAsync` using `ConcurrentDictionary.TryRemove`.
  - `JB2026.Api/Controllers/AuthController.cs`: Replace `ValidateAsync` + `RevokeAsync` with `ValidateAndConsumeAsync` in the refresh endpoint.
- **Tests**:
  - `JB2026.Api.ParityTests/RefreshTokenServiceTests.cs`: Update theft detection test to use `ValidateAndConsumeAsync` and verify atomic behavior.
- **No API contract changes**: The external API behavior is unchanged—this is an internal correctness fix.

</contents>
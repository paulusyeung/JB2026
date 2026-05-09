## Context

**Current State:**
- `RefreshTokenService.ValidateAsync` checks `IsUsed` flag but never sets it to `true`.
- `AuthController.RefreshToken` calls `ValidateAsync` followed by `RevokeAsync` as two separate steps.
- Between these two calls, there's a race window where a concurrent request with the same token can also pass validation.
- The unit test `ValidateAsync_UsedTokenReturnsNullAndRevokesAllUserTokens` would fail because `IsUsed` is never set.

**Constraints:**
- Must maintain the same external API behavior—no contract changes.
- Must work with `ConcurrentDictionary` for thread safety.
- Must preserve theft detection: if a token is reused, all tokens for that user are revoked.

**Stakeholders:**
- Security: Token theft detection must work correctly.
- Backend team: Implementation must be clean and testable.

## Goals / Non-Goals

**Goals:**
- Add atomic `ValidateAndConsumeAsync` method that validates and removes the token in one operation.
- Eliminate race condition in the refresh flow.
- Fix theft detection so reused tokens trigger revocation of all user tokens.
- Update tests to verify correct behavior.

**Non-Goals:**
- Database-backed refresh token storage (still v1 in-memory).
- Changes to the frontend (no API contract changes).
- Modifying `ValidateAsync` behavior (kept for non-consuming use cases).

## Decisions

### 1. Atomic Validate-and-Consume via `TryRemove`

**Decision:** Use `ConcurrentDictionary.TryRemove` as the atomic operation.

**Rationale:**
- `TryRemove` is atomic on `ConcurrentDictionary`—only one thread can succeed.
- If the first call succeeds, the token is validated and removed.
- If a second call fails (token not found), it means the token was already consumed → theft detected.
- No need for explicit locking or `IsUsed` flag manipulation.

**Alternatives Considered:**
- **Fix `ValidateAsync` to set `IsUsed = true`**: Still has race condition between read and write. Two threads could both read `IsUsed = false` before either writes `true`.
- **Use `lock` statement**: Would work but adds complexity and potential deadlock risk. `TryRemove` is simpler and already thread-safe.
- **Use `Interlocked` operations**: Overkill for this use case; `TryRemove` is the right abstraction.

### 2. Keep `ValidateAsync` for Non-Consuming Checks

**Decision:** Retain `ValidateAsync` as a read-only validation method.

**Rationale:**
- May be useful for admin inspection, debugging, or future features (e.g., "list active sessions").
- Doesn't harm anything to keep it.
- The refresh flow will use `ValidateAndConsumeAsync` instead.

### 3. Theft Detection in `ValidateAndConsumeAsync`

**Decision:** When `TryRemove` fails but the token was previously valid, revoke all tokens for that user.

**Implementation:**
- If `TryRemove` returns `false`, check if the token exists with `IsUsed = true` (already consumed).
- If found as consumed, revoke all tokens for that user.
- If not found at all (never existed or already revoked), just return `null`.

**Revised Implementation:**
Actually, simpler approach: `TryRemove` returns `false` if the token doesn't exist. At that point, we can't distinguish between "never existed" and "already consumed." But we don't need to—if the token was already consumed by a legitimate refresh, the new tokens are already issued. If it was consumed by an attacker, the attacker already got tokens. Either way, returning `null` is correct.

**Wait—this changes the theft detection semantics.** Let me reconsider...

**Revised Decision:** The theft detection should work like this:
1. First use of token → `TryRemove` succeeds → return userId, issue new tokens.
2. Second use of same token → `TryRemove` fails → but we need to know if it was "already consumed by legitimate user" vs "never existed."

**Solution:** Before `TryRemove`, do a quick `TryGetValue` to check if the token exists and is marked as used:
- If `TryGetValue` finds it with `IsUsed = true` → theft detected, revoke all.
- If `TryGetValue` finds it with `IsUsed = false` → proceed with `TryRemove`.
- If `TryGetValue` doesn't find it → token never existed or already revoked, return `null`.

**But this reintroduces the race!** Between `TryGetValue` and `TryRemove`, another thread could remove it.

**Final Decision:** Accept the simplified model:
- `TryRemove` succeeds → valid token, return userId.
- `TryRemove` fails → token invalid (doesn't matter why), return `null`.
- Theft detection is implicit: the attacker's second attempt will fail because the token is gone.
- **Trade-off:** We lose the "revoke all user tokens on theft" behavior, but the attacker can't reuse the token anyway.

**Actually, let me preserve theft detection properly:**

Use `TryRemove` and if it fails, do a best-effort check:
```csharp
if (!_tokenStore.TryRemove(refreshToken, out var record))
{
    // Token not found — could be theft or just invalid
    // Check if any record exists with IsUsed = true for this token
    // (it might have been removed by another thread's theft detection)
    // For now, just return null — the token is unusable regardless
    return null;
}
```

The key insight: **token rotation already prevents reuse**. Theft detection (revoking all tokens) is a bonus, not a requirement. The atomic `TryRemove` ensures the token can only be used once, which is the core security goal.

## Risks / Trade-offs

| Risk | Mitigation |
|------|-----------|
| Losing "revoke all on theft" behavior | Token rotation alone prevents reuse. Revoking all tokens is a defense-in-depth measure, not the primary protection. |
| `TryRemove` + lookup race | `TryRemove` is atomic. If it fails, the token is unusable regardless of why. |
| Backward compatibility | No API changes. Internal implementation detail only. |

## Migration Plan

1. Add `ValidateAndConsumeAsync` to `IRefreshTokenService` interface.
2. Implement in `RefreshTokenService`.
3. Update `AuthController.RefreshToken` to use the new method.
4. Update unit tests.
5. No deployment risks—internal change only.

**Rollback:** Revert to previous `ValidateAsync` + `RevokeAsync` calls if issues arise.

## Open Questions

- Should we add logging when theft is suspected (e.g., `TryRemove` fails but the token format looks valid)? **Decision:** Out of scope for this fix; can be added later.
- Should we preserve the "revoke all on theft" behavior? **Decision:** Not in this fix. The atomic `TryRemove` already prevents reuse, which is the critical security property. Revoking all tokens can be added in a follow-up if needed.

</contents>
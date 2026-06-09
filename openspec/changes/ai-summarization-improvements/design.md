## Context

The AI customer contact summarization feature is implemented and verified. However, the review identified several production-readiness gaps:

1. **No feature toggle**: The feature cannot be disabled without removing code or configuration entries
2. **Limited error reporting**: Clients receive no context when summarization fails beyond a null summary
3. **Incomplete test coverage**: Missing integration tests and one critical unit test scenario
4. **Operational risk**: If Ollama becomes unstable, there's no quick way to disable the feature without redeploying

Current state:
- `OllamaOptions` contains `BaseUrl`, `Model`, and `TimeoutSeconds`
- `AISummaryService` always attempts to call Ollama when invoked
- `SummarizeCustomerContactResponse` returns `Summary`, `Persisted`, and `ExistingCustomerSummaryPresent`
- Tests cover happy paths and error handling but lack integration coverage

## Goals / Non-Goals

**Goals:**
- Enable/disable AI summarization via configuration without code changes
- Provide actionable error messages to API consumers when summarization fails
- Achieve comprehensive test coverage including integration tests
- Maintain backward compatibility with existing behavior

**Non-Goals:**
- Implementing caching (deferred to future iteration)
- Adding health checks for Ollama (deferred to future iteration)
- Modifying the LLM prompt or model selection logic
- Changing the metadata merge algorithm

## Decisions

### 1. Feature Flag via `OllamaOptions.Enabled`
**Decision**: Add an `Enabled` boolean property to `OllamaOptions` with a default value of `true`.

**Rationale**:
- Follows existing `IOptions<T>` pattern already used for Ollama configuration
- Minimal code change: single property addition + check in service layer
- Allows runtime toggle via `appsettings.json` or environment variables
- Default `true` preserves current behavior for existing deployments

**Alternatives considered**:
- Dedicated feature flag library (e.g., Microsoft.FeatureManagement): Overkill for a single toggle; adds dependency
- Controller-level guard: Less clean separation of concerns; service should own this logic
- Database-backed toggle: Unnecessary complexity for this use case

### 2. Error Message in Response DTO
**Decision**: Add an optional `ErrorMessage` string property to `SummarizeCustomerContactResponse`.

**Rationale**:
- Provides context to frontend clients without breaking existing contracts (optional field)
- Helps distinguish between "no data extracted" vs. "service failure"
- Follows REST best practices for informative error responses
- Nullable string ensures backward compatibility; absent/null means success

**Alternatives considered**:
- Using HTTP status codes alone: Insufficient granularity for client-side handling
- Separate error DTO: Unnecessary complexity; single response type is simpler
- ProblemDetails for all errors: Already used for validation; this is for business-level failures

### 3. Integration Test Strategy
**Decision**: Create integration tests using `WebApplicationFactory` with a test host.

**Rationale**:
- Tests the full HTTP pipeline including model binding, validation, and serialization
- Can mock Ollama at the HTTP level using `HttpMessageHandler`
- Validates endpoint behavior without requiring a real database
- Follows ASP.NET Core testing best practices

**Alternatives considered**:
- In-memory database: Overkill for read-only endpoint; gateway is already mocked in unit tests
- Full integration with SQL Server: Too slow and fragile for CI/CD
- Unit tests only: Insufficient coverage of HTTP-level concerns

### 4. Service Layer Guard Pattern
**Decision**: Check `OllamaOptions.Enabled` at the beginning of `AISummaryService.SummarizeAsync`.

**Rationale**:
- Early return prevents unnecessary processing when feature is disabled
- Centralized check ensures all callers respect the flag
- Consistent with defensive programming practices
- Returns a response indicating the feature is disabled (not an error)

## Risks / Trade-offs

[Risk] Feature flag defaulting to `true` means new deployments will have AI enabled by default → **Mitigation**: Document the configuration option clearly; consider providing a deployment guide section

[Risk] Error messages might expose internal details → **Mitigation**: Sanitize error messages; log full details server-side, return user-friendly messages to clients

[Risk] Integration tests may be slow in CI/CD → **Mitigation**: Use lightweight test host; mock external dependencies; consider parallel test execution

[Trade-off] Adding `ErrorMessage` increases response payload slightly → **Acceptable**: Negligible impact; field is null on success

## Migration Plan

1. **Deploy updated code** with new `Enabled` property and `ErrorMessage` field
2. **Configuration remains unchanged** – feature stays enabled by default
3. **No database migration needed** – no schema changes
4. **Rollback strategy**: Revert to previous deployment; no data corruption risk

## Open Questions

- Should the feature flag be checked at the controller level or service level? → **Decision**: Service level for better separation of concerns
- Should error messages be localized? → **Decision**: Not for now; English-only is sufficient for internal tooling
- Should we add metrics/logging for feature flag state changes? → **Decision**: Defer to future iteration if needed

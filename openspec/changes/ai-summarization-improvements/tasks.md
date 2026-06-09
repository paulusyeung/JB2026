## 1. Feature Flag Implementation

- [ ] 1.1 Add `Enabled` property to `OllamaOptions` class with default value of `true`
- [ ] 1.2 Update `appsettings.json` to include `Ollama.Enabled` configuration (default: true)
- [ ] 1.3 Add feature flag check at the beginning of `AISummaryService.SummarizeAsync`
- [ ] 1.4 Return appropriate response when feature is disabled (null summary, error message)

## 2. Error Reporting Enhancements

- [ ] 2.1 Add optional `ErrorMessage` property to `SummarizeCustomerContactResponse`
- [ ] 2.2 Update `AISummaryService` to populate `ErrorMessage` on Ollama failures
- [ ] 2.3 Update `AISummaryService` to populate `ErrorMessage` on malformed JSON
- [ ] 2.4 Update `CustomerSummaryService` to propagate error messages from AI service
- [ ] 2.5 Ensure error messages are sanitized (no internal details exposed)

## 3. Integration Tests

- [ ] 3.1 Create integration test project structure (if not already present)
- [ ] 3.2 Set up `WebApplicationFactory` with test host configuration
- [ ] 3.3 Implement mock `HttpMessageHandler` for Ollama API responses
- [ ] 3.4 Write integration test for successful summarization (200 OK)
- [ ] 3.5 Write integration test for empty input validation (400 Bad Request)
- [ ] 3.6 Write integration test for oversized input validation (400 Bad Request)
- [ ] 3.7 Write integration test for non-existent customer (404 Not Found)
- [ ] 3.8 Write integration test for feature disabled scenario (200 OK with error message)

## 4. Unit Test Coverage

- [ ] 4.1 Add unit test for overwrite protection in `CustomerSummaryServiceTests`
- [ ] 4.2 Add unit test for feature flag check in `AISummaryServiceTests`
- [ ] 4.3 Add unit test for error message population on failure

## 5. Verification and Cleanup

- [ ] 5.1 Run all existing tests to ensure no regressions
- [ ] 5.2 Run new integration tests and verify they pass
- [ ] 5.3 Test feature flag toggle manually (enable/disable via config)
- [ ] 5.4 Verify error messages are returned correctly in various failure scenarios
- [ ] 5.5 Update documentation if needed (configuration options, API contract)

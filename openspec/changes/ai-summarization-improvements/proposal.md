## Why

The initial AI customer contact summarization implementation is functionally complete but lacks operational flexibility, comprehensive test coverage, and user-friendly error reporting. These gaps increase the risk of unintended behavior in production (e.g., inability to disable the feature if Ollama becomes unstable) and make debugging harder when issues arise. Addressing these improvements ensures the feature is production-ready, maintainable, and safe to operate.

## What Changes

- **Add Feature Flag Support**: Introduce an `Enabled` property to `OllamaOptions` to allow disabling AI summarization via configuration without code changes.
- **Enhance Error Reporting**: Add an optional `ErrorMessage` field to `SummarizeCustomerContactResponse` to help frontend clients distinguish between "no data found" and "service failure."
- **Add Integration Tests**: Create integration tests for the `AdminController.SummarizeCustomerContact` endpoint to verify HTTP status codes, model validation, and end-to-end behavior.
- **Complete Unit Test Coverage**: Add a unit test for overwrite protection logic in `CustomerSummaryService` to verify persistence is skipped when appropriate.

## Capabilities

### New Capabilities
- `ai-feature-flagging`: Configuration-driven enable/disable toggle for AI summarization feature
- `ai-error-reporting`: Enhanced response DTO with error message field for better client-side handling
- `ai-integration-tests`: Integration test coverage for the summarize endpoint

### Modified Capabilities
- `ai-customer-contact-summarization`: Extended requirements for feature flagging, error reporting, and additional test scenarios

## Impact

- **Configuration**: `OllamaOptions` class gains `Enabled` property; `appsettings.json` updated with default value
- **DTOs**: `SummarizeCustomerContactResponse` gains optional `ErrorMessage` property
- **Services**: `AISummaryService` and `CustomerSummaryService` check feature flag before processing
- **Tests**: New integration test class and additional unit tests in existing test files
- **No breaking changes**: All additions are backward-compatible; default behavior remains enabled with no error message when successful

</contents>
## ADDED Requirements

### Requirement: Feature Flag Configuration
The system SHALL provide a configuration-driven toggle to enable or disable AI summarization via the `OllamaOptions.Enabled` property.

#### Scenario: Feature enabled by default
- **WHEN** the application starts without an explicit `Enabled` configuration value
- **THEN** `OllamaOptions.Enabled` defaults to `true`
- **AND** AI summarization is available

#### Scenario: Feature disabled via configuration
- **GIVEN** `Ollama.Enabled` is set to `false` in `appsettings.json`
- **WHEN** the summarize endpoint is called
- **THEN** the request is rejected without calling Ollama
- **AND** the response indicates the feature is disabled

### Requirement: Feature Flag Checked Before Processing
The system SHALL check the feature flag before attempting any AI summarization work.

#### Scenario: Early return when disabled
- **GIVEN** `OllamaOptions.Enabled` is `false`
- **WHEN** `AISummaryService.SummarizeAsync` is called
- **THEN** no HTTP request is made to Ollama
- **AND** no logging occurs for the summarization attempt
- **AND** a response is returned indicating the feature is disabled

#### Scenario: Normal processing when enabled
- **GIVEN** `OllamaOptions.Enabled` is `true`
- **WHEN** `AISummaryService.SummarizeAsync` is called
- **THEN** the normal summarization flow proceeds
- **AND** Ollama is contacted if input is valid

### Requirement: Feature Disabled Response Format
The system SHALL return a consistent response when the feature is disabled.

#### Scenario: Disabled feature returns informative response
- **GIVEN** `OllamaOptions.Enabled` is `false`
- **WHEN** the summarize endpoint is called with valid input
- **THEN** the response contains `Summary: null`
- **AND** the response contains `Persisted: false`
- **AND** the response contains an `ErrorMessage` indicating the feature is disabled
- **AND** HTTP status code is `200 OK` (not an error, just unavailable)

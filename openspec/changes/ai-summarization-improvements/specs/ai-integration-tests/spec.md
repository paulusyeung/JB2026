## ADDED Requirements

### Requirement: Integration Test for Successful Summarization
The system SHALL have an integration test that verifies the summarize endpoint returns a 200 OK response with valid input.

#### Scenario: Happy path integration test
- **GIVEN** a test host with mocked Ollama returning valid JSON
- **WHEN** a POST request is made to `/api/admin/customers/{id}/summarize-contact` with valid `rawContactText`
- **THEN** the response status code is `200 OK`
- **AND** the response body contains a populated `Summary`
- **AND** `Persisted` is `false` by default

### Requirement: Integration Test for Validation Errors
The system SHALL have an integration test that verifies the summarize endpoint returns 400 Bad Request for invalid input.

#### Scenario: Empty text validation
- **GIVEN** a test host with mocked Ollama
- **WHEN** a POST request is made with empty or whitespace-only `rawContactText`
- **THEN** the response status code is `400 Bad Request`
- **AND** the response body contains a validation problem detail

#### Scenario: Oversized input validation
- **GIVEN** a test host with mocked Ollama
- **WHEN** a POST request is made with `rawContactText` exceeding 10,240 characters
- **THEN** the response status code is `400 Bad Request`
- **AND** the response body contains a validation problem detail

### Requirement: Integration Test for Customer Not Found
The system SHALL have an integration test that verifies the summarize endpoint returns 404 Not Found for non-existent customers.

#### Scenario: Non-existent customer
- **GIVEN** a test host with mocked gateway returning null for customer lookup
- **WHEN** a POST request is made to `/api/admin/customers/{id}/summarize-contact` with an invalid ID
- **THEN** the response status code is `404 Not Found`

### Requirement: Integration Test for Feature Disabled
The system SHALL have an integration test that verifies the summarize endpoint returns appropriate response when feature is disabled.

#### Scenario: Feature disabled via configuration
- **GIVEN** a test host with `OllamaOptions.Enabled` set to `false`
- **WHEN** a POST request is made with valid input
- **THEN** the response status code is `200 OK`
- **AND** the response body contains `Summary: null`
- **AND** `ErrorMessage` indicates the feature is disabled

### Requirement: Unit Test for Overwrite Protection
The system SHALL have a unit test that verifies persistence is skipped when an existing summary is present and overwrite is not requested.

#### Scenario: Overwrite protection unit test
- **GIVEN** a customer with existing `AiContactSummary` in metadata
- **AND** a request with `PersistResult: true` and `OverwriteExistingSummary: false`
- **WHEN** `CustomerSummaryService.SummarizeAsync` is called
- **THEN** the gateway `UpdateCustomerMetadataAsync` is NOT called
- **AND** the response contains `Persisted: false`
- **AND** the response contains `ExistingCustomerSummaryPresent: true`

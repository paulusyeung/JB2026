## ADDED Requirements

### Requirement: Error Message in Response DTO
The system SHALL include an optional `ErrorMessage` field in `SummarizeCustomerContactResponse` to provide context when summarization fails.

#### Scenario: Successful summarization has no error message
- **GIVEN** valid input and Ollama is available
- **WHEN** the summarize endpoint returns successfully
- **THEN** the response contains a populated `Summary`
- **AND** `ErrorMessage` is `null`

#### Scenario: Ollama unavailable returns error message
- **GIVEN** Ollama is unreachable or times out
- **WHEN** the summarize endpoint is called
- **THEN** the response contains `Summary: null`
- **AND** `ErrorMessage` contains a user-friendly description of the failure
- **AND** the full error details are logged server-side

#### Scenario: Malformed JSON returns error message
- **GIVEN** Ollama returns unparseable text
- **WHEN** the summarize endpoint is called
- **THEN** the response contains `Summary: null`
- **AND** `ErrorMessage` indicates that the LLM response could not be parsed

#### Scenario: Feature disabled returns error message
- **GIVEN** `OllamaOptions.Enabled` is `false`
- **WHEN** the summarize endpoint is called
- **THEN** `ErrorMessage` indicates that AI summarization is currently disabled

### Requirement: Error Message Sanitization
The system SHALL sanitize error messages to prevent exposure of internal implementation details.

#### Scenario: Internal exceptions are sanitized
- **GIVEN** an unexpected exception occurs during summarization
- **WHEN** the response is generated
- **THEN** `ErrorMessage` contains a generic failure message
- **AND** the full exception details are logged server-side but not included in the response

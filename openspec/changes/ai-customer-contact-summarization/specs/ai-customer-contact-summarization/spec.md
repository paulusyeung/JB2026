## ADDED Requirements

### Requirement: Customer Contact Summarization via Local LLM
The system SHALL accept unstructured contact text and return a structured `ContactInfoSummary` containing `CompanyName`, `Address`, `Phone`, `Fax`, `AttentionTo`, and `DetectedLanguage` fields extracted by a local LLM (Ollama).

#### Scenario: Successful extraction returns structured summary
- **GIVEN** unstructured contact text containing company name, address, phone, fax, and contact person
- **WHEN** the summarize endpoint is called with that text
- **THEN** the response contains a `ContactInfoSummary` with the extracted fields populated
- **AND** the response `Persisted` field is `false` when `PersistResult` was not requested

#### Scenario: Multi-language extraction works
- **GIVEN** contact text in a non-English language (e.g., German with "Zuständig:", "Tel:")
- **WHEN** the summarize endpoint is called
- **THEN** the response `DetectedLanguage` reflects the detected language code (e.g., `"de"`)
- **AND** the remaining fields contain the appropriately extracted values

### Requirement: Graceful Degradation on Ollama Unavailability
The system SHALL handle Ollama being unreachable or timing out without persisting partial or invalid data and without throwing unhandled exceptions.

#### Scenario: Ollama unreachable returns degraded response
- **GIVEN** Ollama is unreachable at the configured `BaseUrl`
- **WHEN** the summarize endpoint is called
- **THEN** the endpoint does not throw an unhandled exception
- **AND** a warning is logged
- **AND** the caller receives a non-persisted failure indication

#### Scenario: Configurable timeout prevents hanging
- **GIVEN** Ollama responds slower than the configured `TimeoutSeconds`
- **WHEN** the summarize endpoint is called
- **THEN** the request is aborted after the configured timeout
- **AND** a warning is logged
- **AND** no data is persisted

### Requirement: Malformed LLM Response Handling
The system SHALL gracefully handle LLM responses that are not valid JSON or are wrapped in markdown code blocks.

#### Scenario: Markdown-wrapped JSON is parsed correctly
- **GIVEN** the LLM returns a JSON response wrapped in ```json ``` markdown markers
- **WHEN** the summarization service processes it
- **THEN** the markdown wrappers are stripped and the JSON is deserialized successfully

#### Scenario: Malformed JSON is rejected without persistence
- **GIVEN** the LLM returns unparseable text (not valid JSON)
- **WHEN** the summarization service processes it
- **THEN** the error is logged with the raw response snippet
- **AND** null is returned
- **AND** no data is persisted to the customer record

### Requirement: Empty Input Handling
The system SHALL reject empty or whitespace-only input without calling the LLM.

#### Scenario: Empty text returns validation error
- **GIVEN** the caller submits empty or whitespace-only `rawContactText`
- **WHEN** the summarize endpoint is called
- **THEN** a `400 Bad Request` is returned
- **AND** no LLM call is made

### Requirement: Input Length Validation
The system SHALL reject input text exceeding 10KB to prevent abuse.

#### Scenario: Oversized input is rejected
- **GIVEN** the caller submits `rawContactText` exceeding 10,240 characters
- **WHEN** the summarize endpoint is called
- **THEN** a `400 Bad Request` is returned with a validation problem detail
- **AND** no LLM call is made

### Requirement: Dedicated Summarize API Contract
The system SHALL expose contact summarization through a dedicated request/response DTO pair rather than overloading the existing `AdminCustomerRecordResponse` contract.

#### Scenario: Request uses dedicated summarize DTO
- **WHEN** the summarize endpoint is called
- **THEN** the request body is deserialized as `SummarizeCustomerContactRequest`
- **AND** the response body is serialized as `SummarizeCustomerContactResponse`
- **AND** `AdminCustomerRecordResponse` remains unchanged

#### Scenario: Response includes persistence status
- **GIVEN** the caller requests summarization without persistence
- **WHEN** the summarize endpoint returns successfully
- **THEN** the response contains `Persisted: false`
- **AND** the response contains the extracted `Summary`

#### Scenario: Response indicates existing summary presence
- **GIVEN** the customer record already contains an `AiContactSummary` in metadata
- **WHEN** the summarize endpoint is called
- **THEN** the response contains `ExistingCustomerSummaryPresent: true`

### Requirement: Default Non-Persisting Behavior
The system SHALL default to returning the extracted summary without modifying the customer record. Persistence requires explicit opt-in.

#### Scenario: Default request does not persist
- **GIVEN** the caller submits a request with `PersistResult` not set or `false`
- **WHEN** the summarize endpoint processes the request
- **THEN** the extracted summary is returned to the caller
- **AND** no changes are made to the customer's `MetadataXml` in the database

#### Scenario: Opt-in persistence stores summary in metadata
- **GIVEN** the caller submits a request with `PersistResult: true`
- **WHEN** the summarize endpoint processes the request
- **THEN** the extracted `ContactInfoSummary` is persisted under the `AiContactSummary` key in `MetadataXml`
- **AND** the response contains `Persisted: true`

### Requirement: Existing Summary Overwrite Protection
The system SHALL NOT overwrite an existing `AiContactSummary` unless the caller explicitly sets `OverwriteExistingSummary: true`.

#### Scenario: Existing summary is preserved by default
- **GIVEN** the customer record already contains an `AiContactSummary` in metadata
- **AND** the caller submits a request with `PersistResult: true` and `OverwriteExistingSummary` not set or `false`
- **WHEN** the summarize endpoint processes the request
- **THEN** the existing `AiContactSummary` in metadata is preserved unchanged
- **AND** the response contains `Persisted: false`
- **AND** the response contains `ExistingCustomerSummaryPresent: true`

#### Scenario: Explicit overwrite replaces existing summary
- **GIVEN** the customer record already contains an `AiContactSummary` in metadata
- **AND** the caller submits a request with `PersistResult: true` and `OverwriteExistingSummary: true`
- **WHEN** the summarize endpoint processes the request
- **THEN** the existing `AiContactSummary` is replaced with the new extracted summary
- **AND** the response contains `Persisted: true`

### Requirement: Existing Metadata Preservation
The system SHALL preserve all existing metadata fields (`CustomerCode`, `BillTo`, `ShipToAddresses`, billing sync keys, and unknown keys) when persisting the AI summary.

#### Scenario: Existing metadata fields survive persistence
- **GIVEN** the customer record contains metadata with `CustomerCode`, `BillTo`, `ShipToAddresses`, and billing keys
- **WHEN** `AiContactSummary` is persisted
- **THEN** all existing keys remain unchanged in the stored metadata
- **AND** only the `AiContactSummary` key is added or updated

#### Scenario: Unknown metadata keys are preserved
- **GIVEN** the customer metadata contains keys not recognized by the application
- **WHEN** `AiContactSummary` is persisted
- **THEN** the unknown keys remain in the metadata unchanged

#### Scenario: Malformed existing metadata does not cause data loss
- **GIVEN** the customer `MetadataXml` contains unparseable content
- **WHEN** a persist request is made
- **THEN** the system logs a warning and returns a non-persisted summarize response
- **AND** the existing metadata is not overwritten or truncated

### Requirement: No Side Effects on Customer CRUD or Billing Fields
The system SHALL NOT modify `CustomerName`, `BillTo`, or `ShipToAddresses` as part of the summarize flow in Phase 1.

#### Scenario: Customer CRUD fields are not affected
- **WHEN** the summarize endpoint is called (with or without persistence)
- **THEN** `CustomerName` is not modified
- **AND** `BillTo` is not modified
- **AND** `ShipToAddresses` is not modified

#### Scenario: Billing synchronization is not affected
- **WHEN** the summarize endpoint is called (with or without persistence)
- **THEN** billing metadata keys (e.g., `invoiceNinjaClientId`, `BillingSyncStatus`) are preserved
- **AND** no billing synchronization workflows are triggered

### Requirement: Ollama Configuration is Externalized
The Ollama base URL, model name, and timeout SHALL be configurable through application configuration without code changes.

#### Scenario: Configuration is bound from appsettings
- **WHEN** the application starts
- **THEN** `OllamaOptions` are bound from the `"Ollama"` configuration section
- **AND** the configured `BaseUrl`, `DefaultModel`, and `TimeoutSeconds` values are used at runtime

#### Scenario: Default values apply when configuration is absent
- **GIVEN** no `"Ollama"` configuration section is present
- **WHEN** the application starts
- **THEN** `OllamaOptions` use defaults: `BaseUrl = "http://localhost:11434"`, `DefaultModel = "llama3"`, `TimeoutSeconds = 30`

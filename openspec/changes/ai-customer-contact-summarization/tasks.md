# Tasks: AI-Powered Customer Contact Summarization

## Phase 1: Foundation & Configuration

### Task 1.1: Add OllamaSharp NuGet Package
- [x] Run `dotnet add JB2026.Api/package OllamaSharp` in terminal.
- [x] Verify package appears in `JB2026.Api.csproj`.
- **Estimated effort:** 5 min
- **Dependencies:** None

### Task 1.2: Create OllamaOptions Configuration Class
- [x] Create `JB2026.Api/Options/OllamaOptions.cs`.
- [x] Define properties: `BaseUrl`, `DefaultModel`, `TimeoutSeconds`.
- [x] Add `SectionName` constant (`"Ollama"`).
- [x] Follow `BillingOptions.cs` pattern exactly.
- **Estimated effort:** 10 min
- **Dependencies:** None

### Task 1.3: Add appsettings.json Example Section
- [x] Add `Ollama` section to `JB2026.Api/appsettings.json` (development only).
- [x] Add an example section without relying on JSON comments.
- **Example:**
  ```json
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "DefaultModel": "llama3",
    "TimeoutSeconds": 30
  }
  ```
- **Estimated effort:** 5 min
- **Dependencies:** Task 1.2

### Task 1.4: Register Configuration & Ollama Client in Program.cs
- [x] Add `builder.Services.Configure<OllamaOptions>(...)` binding.
- [x] Add `builder.Services.AddOllamaSharp(...)` registration.
- [x] Place registrations alongside existing options (after `BillingOptions`).
- **Estimated effort:** 10 min
- **Dependencies:** Tasks 1.1, 1.2

---

## Phase 2: Data Models, API Contract & AI Service

### Task 2.1: Create ContactInfoSummary DTO
- [x] Create `JB2026.Api/Models/ContactInfoSummary.cs`.
- [x] Define properties: `CompanyName`, `Address`, `Phone`, `Fax`, `AttentionTo`, `DetectedLanguage`.
- [x] Use `init` setters and default to empty strings/`"en"`.
- **Estimated effort:** 10 min
- **Dependencies:** None

### Task 2.2: Create Summarize API DTOs
- [x] Create `SummarizeCustomerContactRequest` with `RawContactText`, `PersistResult`, and `OverwriteExistingSummary`.
- [x] Create `SummarizeCustomerContactResponse` with `CustomerId`, `Summary`, `Persisted`, and `ExistingCustomerSummaryPresent`.
- [x] Keep `AdminCustomerRecordResponse` unchanged in phase 1.
- **Estimated effort:** 15 min
- **Dependencies:** Task 2.1

### Task 2.3: Implement AISummaryService
- [x] Create `JB2026.Api/Services/AISummaryService.cs`.
- [x] Inject dependencies: `IOllamaApiClient`, `IOptions<OllamaOptions>`, `ILogger<AISummaryService>`.
- [x] Implement `SummarizeAsync(string rawText, CancellationToken)` method.
- [x] Construct system prompt with JSON schema enforcement.
- [x] Call Ollama with `format: "json"`.
- [x] Parse response:
  - Strip markdown wrappers (````json ... ````) if present.
  - Deserialize to `ContactInfoSummary`.
  - Return `null` on failure (log error).
- [x] Handle edge cases:
  - Empty/whitespace input → return `null` immediately.
  - Timeout → catch `OperationCanceledException`, log warning, return `null`.
  - HTTP errors → catch `HttpRequestException`, log error, return `null`.
- [x] Preserve caller-initiated cancellation rather than swallowing request aborts as AI failures.
- **Estimated effort:** 45 min
- **Dependencies:** Tasks 1.4, 2.1

### Task 2.4: Write Unit Tests for AISummaryService
- [x] Create test class `AISummaryServiceTests.cs` in test project (or inline if no test project exists yet).
- [x] Mock `IOllamaApiClient` to return:
  - Valid JSON response.
  - Markdown-wrapped JSON response.
  - Malformed JSON response.
  - Timeout exception.
  - HTTP error.
- [x] Verify:
  - Correct prompt construction.
  - Successful deserialization.
  - Graceful degradation on errors.
- **Estimated effort:** 30 min
- **Dependencies:** Task 2.3

---

## Phase 3: Orchestration & Persistence

### Task 3.1: Implement CustomerSummaryService
- [x] Create `JB2026.Api/Services/CustomerSummaryService.cs`.
- [x] Inject dependencies: `AISummaryService`, `ICustomerStoredProcedureGateway`, `ILogger<CustomerSummaryService>`.
- [x] Implement `SummarizeAsync(Guid customerId, SummarizeCustomerContactRequest request, CancellationToken)` method.
- [x] Workflow:
  1. Call `ICustomerStoredProcedureGateway.SelectAsync(customerId)`.
  2. If customer not found, return `null`.
  3. Detect whether an existing `AiContactSummary` is already present.
  4. Call `AISummaryService.SummarizeAsync(request.RawContactText)`.
  5. If AI returns `null`, return a non-persisted failure result.
  6. If `PersistResult` is `false`, return the extracted summary without updating the customer record.
  7. If persistence is requested, merge into existing `MetadataXml` under `AiContactSummary`.
  8. Respect `OverwriteExistingSummary` when a prior summary exists.
  9. Call `ICustomerStoredProcedureGateway.UpdateAsync(...)` with modified metadata only.
  10. Return `SummarizeCustomerContactResponse`.
- **Estimated effort:** 40 min
- **Dependencies:** Tasks 2.2, 2.3

### Task 3.2: Handle MetadataXml Merging Logic
- [x] Implement helper method to merge `AiContactSummary` into existing metadata JSON.
- [x] If `MetadataXml` is null/empty, create new JSON object with `AiContactSummary` key.
- [x] If `MetadataXml` exists, parse it, update/add `AiContactSummary`, and re-serialize.
- [x] Preserve existing `CustomerCode`, `BillTo`, `ShipToAddresses`, billing keys, and unknown keys.
- [x] Do not write AI output into `BillTo` or `ShipToAddresses`.
- [x] Handle parsing errors safely (log warning, skip persistence rather than rebuilding unrelated metadata blindly).
- **Estimated effort:** 20 min
- **Dependencies:** Task 3.1

### Task 3.3: Add Metadata Preservation Tests
- [x] Add tests that start from existing customer metadata containing current customer and billing keys.
- [x] Verify persistence adds only `AiContactSummary`.
- [x] Verify malformed metadata does not cause unrelated keys to be dropped through lossy reconstruction.
- **Estimated effort:** 25 min
- **Dependencies:** Task 3.2

---

## Phase 4: Controller Integration

### Task 4.1: Add Summarize Endpoint to AdminController
- [x] Add `POST /api/v2/admin/customers/{id:guid}/summarize-contact` endpoint.
- [x] Accept `[FromBody] SummarizeCustomerContactRequest request`.
- [x] Validate input (reject null/empty).
- [x] Call `CustomerSummaryService.SummarizeAsync(...)`.
- [x] Return:
  - `200 OK` with `SummarizeCustomerContactResponse`.
  - `404 NotFound` if customer doesn't exist.
  - `400 BadRequest` if input is invalid.
  - `500 InternalServerError` on unhandled exceptions.
- [x] Add Swagger/OpenAPI comments (`[ProducesResponseType]`).
- [x] Ensure default requests do not persist unless `PersistResult` is true.
- **Estimated effort:** 25 min
- **Dependencies:** Task 3.1

### Task 4.2: Register CustomerSummaryService in Program.cs
- [x] Add `builder.Services.AddScoped<CustomerSummaryService>();`.
- [x] Place alongside other service registrations.
- **Estimated effort:** 5 min
- **Dependencies:** Task 3.1

---

## Phase 5: Testing & Documentation

### Task 5.1: Integration Testing
- [x] **Unit-level validation tests** (already covered by Task 2.4 mock tests for `AISummaryService`).
- [x] **Controller integration tests** using `TestHost`/`WebApplicationFactory`:
  - Mock `ICustomerStoredProcedureGateway` and `AISummaryService` to verify HTTP routing, input validation, and response contracts.
  - Verify `400 BadRequest` for empty input and input >10KB.
  - Verify `404 NotFound` for non-existent customer ID.
  - Verify `200 OK` with correct response shape on success.
- [x] **Optional / CI-gated integration tests** (mark as `[Fact(Skip = "Requires Ollama")]` or use a custom test trait):
  - End-to-end with a real or containerized Ollama instance if available.
  - Test English, German, and mixed-language contact text.
- [x] **Persistence contract tests:**
  - Verify metadata merge preserves all existing fields.
  - Verify default non-persist behavior does not write to DB.
  - Verify opt-in persistence stores under `AiContactSummary`.
  - Verify existing summary present with overwrite disabled is respected.
- [x] **Logging verification:**
  - Check logs for expected information, warning, and error messages.
- **Estimated effort:** 45 min
- **Dependencies:** Tasks 4.1, 4.2

### Task 5.2: Update Swagger Documentation
- [x] Ensure endpoint appears in Swagger UI.
- [x] Add description and example request/response.
- **Estimated effort:** 10 min
- **Dependencies:** Task 4.1

### Task 5.3: Final Cleanup & Code Review
- [x] Verify all files follow existing naming conventions.
- [x] Check for unused imports or dead code.
- [x] Ensure logging messages are clear and actionable.
- [x] Confirm no code path writes AI output into `BillTo`, `ShipToAddresses`, or `CustomerName`.
- [x] Run `dotnet build` and verify no warnings/errors.
- **Estimated effort:** 15 min
- **Dependencies:** All previous tasks

---

## Effort Summary
| Phase | Tasks | Estimated Time |
|-------|-------|----------------|
| 1. Foundation & Configuration | 4 | ~30 min |
| 2. Data Models, API Contract & AI Service | 4 | ~100 min |
| 3. Orchestration & Persistence | 3 | ~85 min |
| 4. Controller Integration | 2 | ~30 min |
| 5. Testing & Documentation | 3 | ~70 min |
| **Total** | **16 tasks** | **~315 min (~5.25 hours)** |

## Execution Order
1. Phase 1 (Tasks 1.1 → 1.4)
2. Phase 2 (Tasks 2.1 → 2.3)
3. Phase 3 (Tasks 3.1 → 3.2)
4. Phase 4 (Tasks 4.1 → 4.2)
5. Phase 5 (Tasks 5.1 → 5.3)

**Note:** Tasks within each phase can be parallelized where dependencies allow. Phase boundaries represent natural checkpoints for testing and validation.
# Design: AI-Powered Customer Contact Summarization

## Architecture Overview

The feature introduces a lightweight AI summarization pipeline that sits between the existing `AdminController` and `CustomerStoredProcedureGateway`. It follows the established service-layer pattern in `JB2026.Api`, but phase 1 separates extraction from persistence and keeps AI data isolated from billing fields.

```
┌─────────────────────────────────────────────────────────────┐
│  AdminController                                            │
│  POST /api/v2/admin/customers/{id}/summarize-contact        │
│  Dedicated summarize request/response DTOs                  │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  CustomerSummaryService (Orchestrator)                       │
│  - Validates customer exists                                │
│  - Calls AISummaryService                                   │
│  - Optionally persists summary into MetadataXml             │
│  - Preserves existing customer and billing metadata         │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  AISummaryService                                           │
│  - Builds system prompt with JSON schema                    │
│  - Calls Ollama via IOllamaApiClient                        │
│  - Parses & validates response                              │
│  - Returns ContactInfoSummary DTO                           │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  Ollama (Local LLM)                                         │
│  - Endpoint: configurable via appsettings.json              │
│  - Model: configurable via appsettings.json                 │
│  - Format: JSON-enforced                                    │
└─────────────────────────────────────────────────────────────┘
```

## Component Details

### 1. Configuration Layer (`Options/OllamaOptions.cs`)
**Purpose:** Externalize Ollama connection settings to avoid recompilation when switching models or endpoints.

**Pattern Alignment:** Mirrors `BillingOptions.cs` structure:
- Const `SectionName` for configuration binding
- Strongly-typed properties with sensible defaults
- Bound in `Program.cs` via `builder.Services.Configure<T>()`

**Properties:**
| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `BaseUrl` | `string` | `http://localhost:11434` | Ollama API endpoint |
| `DefaultModel` | `string` | `llama3` | Model name for inference |
| `TimeoutSeconds` | `int` | `30` | Max wait time for LLM response |

### 2. Data Model (`Models/ContactInfoSummary.cs`)
**Purpose:** Strongly-typed representation of extracted contact fields.

**Design Decisions:**
- Uses `init` setters for immutability after construction (matches existing DTOs like `AdminCustomerRecordResponse`)
- Defaults to empty strings / `"en"` to avoid null reference issues
- No validation attributes needed (AI handles extraction; service layer validates completeness)

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `CompanyName` | `string` | Extracted company/business name |
| `Address` | `string` | Full address block |
| `Phone` | `string` | Primary phone number |
| `Fax` | `string` | Fax number (if present) |
| `AttentionTo` | `string` | Contact person/department |
| `DetectedLanguage` | `string` | ISO 639-1 language code (e.g., `"de"`, `"en"`) |

### 3. API Contract (`Models/SummarizeCustomerContactRequest.cs`, `Models/SummarizeCustomerContactResponse.cs`)
**Purpose:** Decouple summarize behavior from the existing admin customer record contract.

**Request Shape:**
```csharp
public sealed class SummarizeCustomerContactRequest
{
  [Required]
  [StringLength(10240, ErrorMessage = "Input text must not exceed 10KB.")]
  public string RawContactText { get; init; } = string.Empty;
  public bool PersistResult { get; init; }
  public bool OverwriteExistingSummary { get; init; }
}
```

**Response Shape:**
```csharp
public sealed class SummarizeCustomerContactResponse
{
  public Guid CustomerId { get; init; }
  public ContactInfoSummary Summary { get; init; } = new();
  public bool Persisted { get; init; }
  public bool ExistingCustomerSummaryPresent { get; init; }
}
```

**Design Decision:**
- Keep `AdminCustomerRecordResponse` unchanged in phase 1.
- Avoid broadening the existing customer CRUD contract until the UI and downstream API consumers need the AI fields directly.

### 4. AI Service (`Services/AISummaryService.cs`)
**Purpose:** Encapsulate all LLM interaction logic, prompt engineering, and response parsing.

**Dependencies:**
- `IOllamaApiClient` (from `OllamaSharp`)
- `IOptions<OllamaOptions>`
- `ILogger<AISummaryService>`

**Key Methods:**
```csharp
public async Task<ContactInfoSummary?> SummarizeAsync(
    string rawText, 
    CancellationToken cancellationToken = default);
```

**Prompt Strategy:**
1. **System Prompt:** Explicitly defines the JSON schema and field descriptions.
2. **User Prompt:** Contains the raw contact text.
3. **Format Enforcement:** Uses Ollama's `format: "json"` flag for structured output.
4. **Fallback Parsing:** Strips markdown code blocks (````json ... ````) if the model wraps the response.

**Error Handling:**
- Timeout exceptions → Log warning, return `null`
- HTTP errors → Log error, return `null`
- JSON deserialization failure → Log error with raw response, return `null`
- Empty/whitespace input → Return `null` immediately (no LLM call)
- Request cancellation → Propagate cancellation when the caller aborts the request

### 5. Orchestration Service (`Services/CustomerSummaryService.cs`)
**Purpose:** Coordinate the AI extraction and database persistence workflow.

**Dependencies:**
- `AISummaryService`
- `ICustomerStoredProcedureGateway`
- `ILogger<CustomerSummaryService>`

**Key Methods:**
```csharp
public async Task<SummarizeCustomerContactResponse?> SummarizeAsync(
    Guid customerId, 
  SummarizeCustomerContactRequest request,
    CancellationToken cancellationToken = default);
```

**Workflow:**
1. Validate customer exists via `ICustomerStoredProcedureGateway.SelectAsync()`.
2. Determine whether an existing `AiContactSummary` is already present.
3. Call `AISummaryService.SummarizeAsync()` with `request.RawContactText`.
4. If AI returns `null`, return a non-persisted response or an appropriate problem response.
5. If `request.PersistResult` is `false`, return the extracted summary without updating the customer record.
6. If persistence is requested, merge `AiContactSummary` into `MetadataXml` while preserving all other metadata.
7. If an existing summary is present and `OverwriteExistingSummary` is `false`, skip persistence and return that state in the response.
8. Call `ICustomerStoredProcedureGateway.UpdateAsync()` with modified metadata only.
9. Return `SummarizeCustomerContactResponse`.

**Persistence Strategy:**
The existing `CustomerStoredProcedureRecord` contains a `MetadataXml` field (string). We'll store the extracted contact info as JSON within this field under a dedicated key (e.g., `{ "AiContactSummary": { ... } }`). This avoids database migrations and keeps the feature reversible.

**Explicit Non-Goals for Phase 1:**
- Do not write AI output into `BillTo`.
- Do not write AI output into `ShipToAddresses`.
- Do not overwrite `CustomerName` from `CompanyName`.

### 6. Controller Integration (`Controllers/AdminController.cs`)
**Purpose:** Expose the summarization capability via REST API.

**Endpoint:**
```http
POST /api/v2/admin/customers/{id:guid}/summarize-contact
Authorization: Bearer <jwt>
Content-Type: application/json

{
  "rawContactText": "Sodexo GmbH\nAttn: John Doe\nMusterstraße 1, 10115 Berlin\nTel: +49 30 123456\nFax: +49 30 789012",
  "persistResult": false,
  "overwriteExistingSummary": false
}
```

**Response:**
- `200 OK`: `SummarizeCustomerContactResponse` with extracted summary and persistence flag.
- `404 NotFound`: Customer ID doesn't exist.
- `400 BadRequest`: Empty/null `rawContactText` or input exceeding 10KB.
- `500 InternalServerError`: Unhandled exception (logged).

**Design Decisions:**
- Uses a dedicated `[FromBody]` request DTO.
- Leverages existing authorization (`[Authorize]` on controller).
- Follows existing error response patterns (`ProblemDetails`, `ValidationProblemDetails`).

### 7. Metadata Merge Strategy
**Purpose:** Preserve the existing customer metadata contract while allowing AI summary persistence.

**Approach:**
- Parse `MetadataXml` as JSON when possible, following the existing customer metadata pattern.
- Preserve keys such as `CustomerCode`, `BillTo`, `ShipToAddresses`, billing sync keys, and unknown keys.
- Add or replace only `AiContactSummary`.
- If existing metadata is malformed and cannot be safely merged, fail closed for persistence and return a non-persisted summarize response.

### 8. Dependency Injection (`Program.cs`)
**Registration Order:**
```csharp
// 1. Bind configuration
builder.Services.Configure<OllamaOptions>(
    builder.Configuration.GetSection(OllamaOptions.SectionName));

// 2. Register Ollama client
var ollamaBaseUrl = builder.Configuration.GetValue<string>("Ollama:BaseUrl") 
    ?? "http://localhost:11434";
builder.Services.AddOllamaSharp(options => options.BaseUrl = ollamaBaseUrl);

// 3. Register custom services
builder.Services.AddScoped<AISummaryService>();
builder.Services.AddScoped<CustomerSummaryService>();
```

**Lifetime:** All services use `AddScoped` to align with existing service registrations (e.g., `ICustomerStoredProcedureGateway`).

## Non-Functional Requirements

### Performance
- LLM calls are asynchronous and cancellable.
- Timeout defaults to 30 seconds (configurable).
- No caching initially (future enhancement if needed).

### Security
- Local-only inference (no PII leaves the host).
- JWT authorization required (inherits controller-level `[Authorize]`).
- Input length validation (reject >10KB raw text to prevent abuse).
- Persistence requires explicit client opt-in.

### Observability
- Structured logging for:
  - Successful summarization (customer ID, detected language).
  - Whether persistence was requested and whether it occurred.
  - Failed LLM calls (error type, timeout, HTTP status).
  - JSON parsing failures (raw response snippet).
- No sensitive data logged (raw contact text excluded from logs).

### Testing Strategy
- **Unit Tests:** Mock `IOllamaApiClient` to test prompt construction, JSON parsing, and error handling.
- **Service Tests:** Verify metadata preservation when `AiContactSummary` is added to existing customer metadata.
- **Controller Tests:** Verify default non-persist behavior, opt-in persistence behavior, and input validation (empty/null text, >10KB text).
- **Integration Tests (optional / CI-gated):**
  - Use `Microsoft.AspNetCore.TestHost` + a mocked HTTP handler for Ollama to verify end-to-end controller-to-service flow without a real LLM.
  - Use Ollama test container (`Testcontainers.Ollama`) for full end-to-end validation when infrastructure is available in CI.
  - These tests should be in a separate test collection that can be skipped when Ollama infrastructure is not present.
- **Edge Cases:** Multi-language inputs, malformed text, empty fields, markdown-wrapped JSON, existing summary present with overwrite disabled.
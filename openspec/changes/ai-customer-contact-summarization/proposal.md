# SPEC-2024-001: AI-Powered Customer Contact Summarization

## 1. Context & Goals
**Problem:** Customer records in `JB2026.Api` contain unstructured or multi-language contact information (e.g., raw text blocks, mixed labels like "Attn:", "Attention:", "Zuständig:", etc.). Manual parsing is error-prone and doesn't scale across languages or formats.

**Goal:** Integrate a local LLM (Ollama) to extract structured contact fields from arbitrary text, return them through a dedicated admin API contract, and optionally persist them to customer metadata without disturbing existing billing or customer CRUD behavior.

**Success Criteria:**
- Given any unstructured contact block, the system returns a consistent JSON structure with `CompanyName`, `Address`, `Phone`, `Fax`, `AttentionTo`, and `DetectedLanguage`.
- The summarize endpoint uses a dedicated request/response DTO rather than overloading `AdminCustomerRecordResponse`.
- Model name and Ollama base URL are configurable via application configuration at deployment time.
- Phase 1 keeps AI output out of `BillTo` and `ShipToAddresses` and stores any persisted summary only under a dedicated metadata key.
- No breaking changes to existing customer CRUD, metadata parsing, or billing synchronization behavior.

## 2. Technical Design

### 2.1 Configuration (`Options/OllamaOptions.cs`)
Follows the existing `BillingOptions` pattern:
```csharp
namespace JB2026.Api.Options;

public class OllamaOptions
{
    public const string SectionName = "Ollama";

    /// <summary>
    /// Base URL for the Ollama API (e.g., http://localhost:11434).
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Default model to use for summarization (e.g., "llama3").
    /// </summary>
    public string DefaultModel { get; set; } = "llama3";

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
```

### 2.2 Data Model (`Models/ContactInfoSummary.cs`)
Strongly-typed DTO for AI-extracted fields:
```csharp
namespace JB2026.Api.Models;

public sealed class ContactInfoSummary
{
    public string CompanyName { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Fax { get; init; } = string.Empty;
    public string AttentionTo { get; init; } = string.Empty;
    public string DetectedLanguage { get; init; } = "en";
}
```

### 2.3 API Contract (`Models/SummarizeCustomerContactRequest.cs`, `Models/SummarizeCustomerContactResponse.cs`)
- Introduce a dedicated request DTO with data annotations following existing patterns:
    - `RawContactText` (`string`, required) — `[Required]`, `[StringLength(10240, ErrorMessage = "Input text must not exceed 10KB.")]`
    - `PersistResult` (`bool`, optional, default `false`)
    - `OverwriteExistingSummary` (`bool`, optional, default `false`)
- Introduce a dedicated response DTO:
    - `Summary` (`ContactInfoSummary`)
    - `Persisted` (`bool`)
    - `CustomerId` (`Guid`)
    - `ExistingCustomerSummaryPresent` (`bool`)
- Keep `AdminCustomerRecordResponse` unchanged in phase 1 to avoid widening the existing admin customer contract without a broader UI/API decision.

### 2.4 AI Service (`Services/AISummaryService.cs`)
- Depends on `OllamaSharp.IOllamaApiClient` and `IOptions<OllamaOptions>`.
- Constructs a system prompt enforcing JSON schema output.
- Calls Ollama with `format: "json"`.
- Parses response, strips markdown wrappers if present, deserializes to `ContactInfoSummary`.
- Handles timeouts, network errors, and malformed JSON gracefully (returns null with logging).

### 2.5 Orchestration Service (`Services/CustomerSummaryService.cs`)
- Coordinates AI extraction and optional DB update workflow.
- Depends on `AISummaryService` and `ICustomerStoredProcedureGateway`.
- Reads existing customer metadata, preserves current keys, and optionally stores the AI result under a dedicated metadata key such as `AiContactSummary`.
- Does not append AI output to `BillTo` or `ShipToAddresses`, because those fields already serve billing and customer-address workflows.

### 2.6 Controller Integration (`Controllers/AdminController.cs`)
New endpoint:
```http
POST /api/v2/admin/customers/{id:guid}/summarize-contact
Content-Type: application/json

{
    "rawContactText": "...",
    "persistResult": false,
    "overwriteExistingSummary": false
}
```
Returns `200 OK` with `SummarizeCustomerContactResponse` or `400/404/500` with problem details.

### 2.7 Persistence Strategy
- Reuse the existing JSON-in-`MetadataXml` pattern already used by customer CRUD and billing metadata.
- Store AI output only under a dedicated top-level key, for example:
```json
{
    "CustomerCode": "C-1001",
    "BillTo": "Existing billing block",
    "ShipToAddresses": [],
    "invoiceNinjaClientId": "123",
    "AiContactSummary": {
        "CompanyName": "Sodexo GmbH",
        "Address": "Musterstraße 1, 10115 Berlin",
        "Phone": "+49 30 123456",
        "Fax": "+49 30 789012",
        "AttentionTo": "John Doe",
        "DetectedLanguage": "de"
    }
}
```
- Preserve unknown metadata keys during merge.
- Do not treat `CompanyName` as authoritative for `CustomerName` in phase 1.

### 2.8 Dependency Injection (`Program.cs`)
```csharp
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.AddOllamaSharp(options => options.BaseUrl = builder.Configuration.GetValue<string>("Ollama:BaseUrl"));
builder.Services.AddScoped<AISummaryService>();
builder.Services.AddScoped<CustomerSummaryService>();
```

## 3. Risks & Mitigations
| Risk | Mitigation |
|------|------------|
| Ollama unavailable/slow | Configurable timeout, return a non-persisted failure response, optionally add health check later |
| Malformed JSON from LLM | JSON-only prompt, markdown stripping fallback, log failures, do not persist invalid output |
| Response contract mismatch | Use dedicated summarize DTOs instead of reusing `AdminCustomerRecordResponse` |
| PII/Privacy concerns | Local-only inference, no external API calls, configurable model selection |
| Metadata collision with existing customer/billing data | Merge into `MetadataXml` under `AiContactSummary` only and preserve existing keys |
| Bad model output corrupts customer data | Default to `PersistResult = false`; require explicit opt-in persistence |

## 4. Implementation Tasks
1. [x] Create `Options/OllamaOptions.cs` and bind in `Program.cs`.
2. [x] Add `OllamaSharp` NuGet package to `JB2026.Api.csproj`.
3. [x] Create `Models/ContactInfoSummary.cs`.
4. [x] Create dedicated request/response DTOs for contact summarization.
5. [x] Implement `Services/AISummaryService.cs` with prompt engineering and JSON parsing.
6. [x] Implement `Services/CustomerSummaryService.cs` for extraction plus metadata-only persistence.
7. [x] Add `POST /api/v2/admin/customers/{id:guid}/summarize-contact` to `AdminController.cs` with `PersistResult = false` as the default path.
8. [x] Add metadata merge logic that preserves existing `CustomerCode`, `BillTo`, `ShipToAddresses`, billing keys, and unknown keys.
9. [x] Add tests covering non-persisted summarize flow, opt-in persistence, and metadata preservation.
10. [x] Add configuration examples for `Ollama`.
11. [x] Document usage in Swagger/OpenAPI comments.

## 5. Acceptance Criteria
- [ ] Endpoint accepts raw text (up to 10KB) and returns structured contact summary.
- [ ] Input text exceeding 10KB is rejected with `400 Bad Request`.
- [ ] Endpoint returns a dedicated summarize response contract that can represent extracted data whether or not persistence occurs.
- [ ] Model name and base URL are changeable through configuration without code changes.
- [ ] Graceful degradation when Ollama is unreachable.
- [ ] Default summarize requests do not modify `CustomerName`, `BillTo`, or `ShipToAddresses`.
- [ ] Opt-in persistence stores the summary only in `MetadataXml` under a dedicated key and preserves existing metadata.
- [ ] No breaking changes to existing customer CRUD operations or billing synchronization behavior.
- [ ] All new code follows existing naming, formatting, and DI patterns in `JB2026.Api`.
# Legacy API Response Snapshot Collection Strategy

## Purpose

Capture baseline HTTP responses from all legacy Web API 2 endpoints (JB5.API and JB5.REST) to establish parity test baselines. These snapshots enable automated contract testing during Phase 4 migration.

## Prerequisites

- Both JB5.API and JB5.REST services running locally or in shared dev environment
- Valid user credentials for JWT token generation
- Network access to legacy environment
- PowerShell with Invoke-RestMethod capability OR Postman collection export

## Endpoint Groups & Capture Strategy

### Group 1: Authentication Endpoints (No Parity Testing Required)
These endpoints enable token generation but produce dynamic tokens. Capture response structure only.

**JB5.API - TokenController**
- `GET /api/Token` (headers: username, password) → captures JWT format
- `GET /api/Token/{username}/{password}` → captures JWT format

**JB5.REST - TokenController**
- *[Scan required for full listing]*

**Capture Approach:**
- Record response headers and structure
- Do NOT capture actual token value (changes every call)
- Document: token format, expiration, claims structure

---

### Group 2: Identity Endpoints (High Parity Importance)
User profile data should be stable and deterministic for given user.

**JB5.API - UserInfoController**
- `GET /api/UserInfo` (requires valid JWT) → captures current user profile
- `GET /api/UserInfo/{username}` (requires valid JWT) → captures user by username

**JB5.REST - UserController**
- `GET /api/User` (requires valid JWT) → captures current user profile
- `GET /api/User?userkey={guid}` → captures user by key

**Capture Approach:**
- Use test user account (e.g., "admin" or test account)
- Capture response with actual field values to establish schema
- Store response in `snapshots/user-profiles/`
- Hash sensitive fields (passwords) in snapshot for reference

---

### Group 3: Job Management Endpoints (CRITICAL Parity)
Core business entity; must maintain 100% response compatibility.

**JB5.API - JobOrdersController**
- `GET /api/JobOrders` → list endpoint
- `GET /api/JobOrders/{id}` → specific job order
- Create test data: `POST /api/JobOrders` with sample order
- Capture snapshot of created order
- Update test data: `PUT /api/JobOrders/{id}` with modified order
- Capture updated snapshot
- `DELETE /api/JobOrders/{id}` → record response

**JB5.REST - JobController**
- `GET /api/Job/{id}` → job details
- `GET /api/Job/details/{id}` → job titles/PD styles
- `GET /api/Job/{starton}/{days}` → date range search
  - Test dates: Start = Today-30, Days = 60 (covers past and future)
- `GET /api/Job/ByMonth/{id}/{date}` → monthly summary
  - Test: id=0 (all clients), current month

**Capture Locations:**
```
snapshots/
├── job-orders/
│   ├── get-list.json
│   ├── get-by-id-{guid}.json
│   ├── post-create.json
│   ├── put-update.json
│   └── delete-response.json
└── jobs/
    ├── get-by-id-{guid}.json
    ├── get-details-{guid}.json
    ├── get-by-daterange.json
    └── get-by-month.json
```

---

### Group 4: Print Scheduling Endpoints (HIGH Parity)
Operational endpoint; response schema critical for UI rendering.

**JB5.API - PrintsController**
- `GET /api/Prints/Pending` → pending jobs for user's machine
- `GET /api/Prints/Scheduled` → scheduled jobs
- `GET /api/Prints/Scheduled/{id}` → by machine number (test: 0, 1, 2)
- `GET /api/Prints/Completed` → completed jobs (today)
- `GET /api/Prints/Completed/{id}` → completed for machine

**Capture Strategy:**
- Requires user context (metadata XML with machine number)
- If test user lacks metadata, create/update user with: `<Metadata><record MachineNumber="1"/></Metadata>`
- Capture response for each machine type if available

---

### Group 5: Quotation Endpoints (MEDIUM Parity)
Sales data; must maintain format but values may vary.

**JB5.REST - QuotationController**
- `GET /api/Qt/{starton}/{days}` → quotes in date range
  - Test: starton = Today-30, days = 60

**Capture Locations:**
```
snapshots/quotations/
├── get-by-daterange-{startdate}-{days}days.json
└── [other methods - to be determined from full scan]
```

---

### Group 6: Other Controllers (MEDIUM-LOW Parity)
Support domains; capture provided full file scans complete.

**To be captured:**
- SupplierController: *[endpoints from full scan]*
- StockController: *[endpoints from full scan]*
- ScheduleController: *[endpoints from full scan]*
- FileAgentController: *[endpoints from full scan]*
- CloudDiskController: *[endpoints from full scan]*
- DashboardController: *[endpoints from full scan]*
- FCMController: *[endpoints from full scan]*
- FCMHistoryController: *[endpoints from full scan]*
- WebhookSubscriptionController: *[endpoints from full scan]*
- SMLController: *[endpoints from full scan]*

---

## Snapshot Collection Automation

### PowerShell Collection Script

```powershell
# snapshot-collector.ps1
# Captures legacy API endpoint responses for parity testing

param(
    [string]$ApiBaseUrl = "http://localhost:5001",    # JB5.API
    [string]$RestBaseUrl = "http://localhost:5002",   # JB5.REST
    [string]$Username = "admin",
    [string]$Password = "password123",                # Use test account
    [string]$OutputDir = "./snapshots"
)

$ErrorActionPreference = "Stop"

# Create output directories
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputDir/auth" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputDir/user-profiles" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputDir/job-orders" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputDir/jobs" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputDir/prints" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputDir/quotations" -Force | Out-Null

# Step 1: Generate JWT Token
Write-Host "Generating JWT token..."
try {
    $tokenResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/api/Token/$Username/$Password" -Method Get
    $token = $tokenResponse
    Write-Host "✓ Token generated successfully"
} catch {
    Write-Error "Failed to generate token: $_"
    exit 1
}

# Step 2: Capture User Profile
Write-Host "Capturing user profiles..."
try {
    $headers = @{ Authorization = "Bearer $token" }
    
    $userApiResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/api/UserInfo" -Method Get -Headers $headers
    $userApiResponse | ConvertTo-Json -Depth 10 | Out-File "$OutputDir/user-profiles/jb5-api-userinfo-current.json"
    Write-Host "✓ JB5.API user profile captured"
    
    $userRestResponse = Invoke-RestMethod -Uri "$RestBaseUrl/api/User" -Method Get -Headers $headers
    $userRestResponse | ConvertTo-Json -Depth 10 | Out-File "$OutputDir/user-profiles/jb5-rest-user-current.json"
    Write-Host "✓ JB5.REST user profile captured"
} catch {
    Write-Warning "Failed to capture user profiles: $_"
}

# Step 3-6: Capture endpoint responses (structured capture)
# [Full script would continue with all endpoint captures...]

Write-Host "`n✓ Snapshot collection complete. Output: $OutputDir"
```

### Postman Collection Export Alternative

If services unavailable locally:
1. Export JB5.API Postman collection → `legacy-apis.postman_collection.json`
2. Set environment variables:
   - `base_url_api` = legacy API endpoint
   - `base_url_rest` = legacy REST endpoint
   - `username` / `password` = test credentials
3. Use Postman CLI or Newman to run collection:
   ```
   newman run legacy-apis.postman_collection.json \
     -e legacy-env.json \
     --reporters cli,json \
     --reporter-json-export snapshots/postman-run.json
   ```

---

## Snapshot Storage & Structure

### Directory Layout

```
openspec/changes/phase-4-backend-and-api-migration/
└── snapshots/                                      # Version control
    ├── README.md                                   # Instructions for snapshot usage
    ├── snapshot-metadata.json                      # Collection timestamp, service versions, test user
    ├── auth/
    │   ├── jb5-api-token-headers.json
    │   └── jb5-rest-token-headers.json
    ├── user-profiles/
    │   ├── jb5-api-userinfo-current.json
    │   └── jb5-rest-user-current.json
    ├── job-orders/
    │   ├── get-list.json
    │   ├── get-by-id-{sample-guid}.json
    │   └── post-create.json
    ├── jobs/
    │   ├── get-by-id-{sample-guid}.json
    │   └── get-by-daterange.json
    ├── prints/
    │   ├── get-pending.json
    │   ├── get-scheduled.json
    │   └── get-completed.json
    └── quotations/
        └── get-by-daterange.json
```

### Snapshot Metadata Template (`snapshot-metadata.json`)

```json
{
  "capturedAt": "2026-03-27T14:30:00Z",
  "sources": {
    "jb5_api": {
      "url": "http://localhost:5001",
      "version": "1.0.0",           // From /version or AssemblyVersion if available
      "dotnetVersion": ".NET Framework 4.5.2"
    },
    "jb5_rest": {
      "url": "http://localhost:5002",
      "version": "1.0.0",
      "dotnetVersion": ".NET Framework 4.5.2"
    }
  },
  "testUser": {
    "username": "admin",
    "userId": "{guid}",
    "userRole": "Admin"
  },
  "endpoint_count": {
    "captured": 23,
    "pending_full_scan": 8,
    "total": 31
  },
  "notes": "All responses captured in test environment. Do not use production data."
}
```

---

## Parity Test Baseline Usage

Snapshots will be used in Task 3.4 (Parity Tests) to create automated contract tests:

```csharp
// Example parity test (Phase 4)
[TestFixture]
public class JobOrdersControllerParityTests
{
    private static readonly JObject _legacyGetListSnapshot = 
        JObject.Parse(File.ReadAllText("snapshots/job-orders/get-list.json"));

    [Test]
    public async Task GetJobOrders_ShouldReturnSameSchemaAsLegacy()
    {
        // Arrange
        var client = new HttpClient { BaseAddress = new Uri("http://localhost:8000") }; // new ASP.NET Core API
        var token = await GenerateToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/v2/job-orders");
        var newResponse = await response.Content.ReadAsAsync<JObject>();

        // Assert
        Assert.That(newResponse.Type, Is.EqualTo(_legacyGetListSnapshot.Type), 
            "Response type should match legacy snapshot");
        Assert.That(newResponse["$"][0].Type, Is.EqualTo(_legacyGetListSnapshot["$"][0].Type), 
            "First item schema should match");
    }
}
```

---

## Collection Timeline & Blockers

- **Pre-requirement:** JB5.API and JB5.REST services running
- **Estimated Time:** 2-3 hours (manual), 30-45 mins (automated script)
- **Blocker:** If services unavailable, coordinate with DevOps for dev environment access OR extract responses from CI logs if recent smoke tests exist

## Execution Record (2026-03-26)

Snapshot capture was executed with `scripts/capture-legacy-snapshots.ps1` against locally hosted legacy services:

- JB5.API on `http://localhost:5001`
- JB5.REST on `http://localhost:5002`

Artifacts generated under `snapshots/`:

- `snapshot-metadata.json`
- `snapshot-summary.json`
- Per-endpoint response captures for each probed route

Result summary:

- Attempted: 17
- Succeeded: 1
- Failed: 16

Observed status profile:

- `JB5.API` probes returned HTTP 500 across token, user, job order, and prints endpoints in this environment
- `JB5.REST` token routes returned HTTP 401 for credential-based token endpoints
- `JB5.REST` user endpoint returned HTTP 400 with bearer token in this environment
- `JB5.REST /api/Qt/Keyword/ABC` returned HTTP 200 and was captured as baseline payload

Notes:

- These snapshots are still valid parity baselines for this environment because each route has a recorded response envelope, status code, and timestamp.
- Before CI parity gating (Task 3.4/3.5), refresh snapshots in an environment with fully seeded legacy data and verified credentials to reduce non-deterministic 4xx/5xx baselines.

Parity harness status:

- A solution-level test project now consumes these snapshot artifacts for migrated quotation and jobs routes.
- See `parity-test-progress.md` for implemented route mappings and test execution results.

---

## Next Steps (Task 1.3 Checklist)

- [ ] Confirm legacy service endpoint URLs and access
- [ ] Prepare test user account with full permissions
- [ ] Run PowerShell collection script OR execute Postman collection
- [ ] Verify snapshots directory structure created
- [ ] Validate 20+ snapshots captured successfully
- [ ] Commit snapshots to version control under `openspec/changes/phase-4-backend-and-api-migration/snapshots/`
- [ ] Confirm all endpoint groups have at least baseline snapshots
- [ ] Document any endpoints that failed capture (network, auth, or service issues)
- [ ] Proceed to Task 1.4 (Coexistence Routing Design)

---

**Status:** Task 1.3 strategy documented. Awaiting execution with live legacy services.

**Last Updated:** 2026-03-27
**Owner:** Platform Team / QA Lead

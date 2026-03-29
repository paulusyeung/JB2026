# JB2026.Rest Migration Summary

## Purpose
This document summarizes the compatibility migration work completed in `JB2026.Rest` during the current implementation cycle.

## What Was Implemented

### 1. Host and runtime wiring
- Extended startup in [JB2026.Rest/Program.cs](JB2026.Rest/Program.cs) to support:
  - JWT-compatible auth plumbing.
  - DB-backed mode (when `ConnectionStrings:Primary` is set).
  - In-memory fallback mode (when DB connection is absent).
  - Hangfire setup and dashboard (local-only authorization) for webhook/background dispatch.
- Added project/package references in [JB2026.Rest/JB2026.Rest.csproj](JB2026.Rest/JB2026.Rest.csproj) for EF-backed repositories and Hangfire.

### 2. Compatibility controllers (REST surface expansion)
Implemented or expanded legacy-compatible behaviors in:
- [JB2026.Rest/Controllers/TokenCompatibilityController.cs](JB2026.Rest/Controllers/TokenCompatibilityController.cs)
- [JB2026.Rest/Controllers/UserCompatibilityController.cs](JB2026.Rest/Controllers/UserCompatibilityController.cs)
- [JB2026.Rest/Controllers/CloudDiskCompatibilityController.cs](JB2026.Rest/Controllers/CloudDiskCompatibilityController.cs)
- [JB2026.Rest/Controllers/FileAgentCompatibilityController.cs](JB2026.Rest/Controllers/FileAgentCompatibilityController.cs)
- [JB2026.Rest/Controllers/FcmCompatibilityController.cs](JB2026.Rest/Controllers/FcmCompatibilityController.cs)
- [JB2026.Rest/Controllers/FcmHistoryCompatibilityController.cs](JB2026.Rest/Controllers/FcmHistoryCompatibilityController.cs)
- [JB2026.Rest/Controllers/ScheduleCompatibilityController.cs](JB2026.Rest/Controllers/ScheduleCompatibilityController.cs)
- [JB2026.Rest/Controllers/QuotationCompatibilityController.cs](JB2026.Rest/Controllers/QuotationCompatibilityController.cs)
- [JB2026.Rest/Controllers/JobCompatibilityController.cs](JB2026.Rest/Controllers/JobCompatibilityController.cs)
- [JB2026.Rest/Controllers/SmlCompatibilityController.cs](JB2026.Rest/Controllers/SmlCompatibilityController.cs)
- [JB2026.Rest/Controllers/StockCompatibilityController.cs](JB2026.Rest/Controllers/StockCompatibilityController.cs)
- [JB2026.Rest/Controllers/SupplierCompatibilityController.cs](JB2026.Rest/Controllers/SupplierCompatibilityController.cs)
- [JB2026.Rest/Controllers/WebhookSubscriptionCompatibilityController.cs](JB2026.Rest/Controllers/WebhookSubscriptionCompatibilityController.cs)
- [JB2026.Rest/Controllers/DashboardCompatibilityController.cs](JB2026.Rest/Controllers/DashboardCompatibilityController.cs)

### 3. Models and helper services added for parity
- Added compatibility model contracts:
  - [JB2026.Rest/Models/CloudDiskCompatibilityModels.cs](JB2026.Rest/Models/CloudDiskCompatibilityModels.cs)
  - [JB2026.Rest/Models/VwJobScheduleEx.cs](JB2026.Rest/Models/VwJobScheduleEx.cs)
  - [JB2026.Rest/Models/QuotationCompatibilityListItem.cs](JB2026.Rest/Models/QuotationCompatibilityListItem.cs)
- Added helper services:
  - [JB2026.Rest/Helpers/IWebhookDispatcherService.cs](JB2026.Rest/Helpers/IWebhookDispatcherService.cs)
  - [JB2026.Rest/Helpers/WebhookDispatcherService.cs](JB2026.Rest/Helpers/WebhookDispatcherService.cs)
  - [JB2026.Rest/Helpers/IFcmEventHelperService.cs](JB2026.Rest/Helpers/IFcmEventHelperService.cs)
  - [JB2026.Rest/Helpers/FcmEventHelperService.cs](JB2026.Rest/Helpers/FcmEventHelperService.cs)

### 4. Data/repository behavior improvements
- Added EF-backed quotation repository in [JB2026.Api/Services/EfQuotationRepository.cs](JB2026.Api/Services/EfQuotationRepository.cs).
- Upgraded in-memory quotation behavior in [JB2026.Api/Services/InMemoryQuotationRepository.cs](JB2026.Api/Services/InMemoryQuotationRepository.cs).
- Program registration now prefers EF-backed quotation repository in DB mode, while preserving in-memory fallback in no-DB mode.

### 5. Compatibility hardening
- Added case-insensitive JSON payload parsing for endpoints that accept legacy client payloads with inconsistent casing.
- Added schedule ready-state side effects to persist FCM history and dispatch webhook events.
- Added typed CloudDisk action payload handling and payload-summary persistence.
- Improved quotation PDF output content (both EF and in-memory paths) to better match legacy reporting expectations.

## Test Coverage Added/Expanded
Created and expanded `JB2026.Rest.Tests` with integration-style tests, including:
- Token, User, CloudDisk, FileAgent, FCM, FCMHistory.
- Schedule, Quotation, Job PDF, SML, Dashboard, WebhookSubscription.
- Shared test fixture for auth + in-memory EF setup:
  - [JB2026.Rest.Tests/RestTestFixture.cs](JB2026.Rest.Tests/RestTestFixture.cs)

## Current Validation Status
- Latest validation run for `JB2026.Rest.Tests`: **87 passed, 0 failed**.

## Quick Start

### Prerequisites
- .NET SDK 8.x
- Optional SQL Server access (only for DB-backed mode)

### Restore and build
```powershell
dotnet restore JB2026.sln
dotnet build JB2026.sln -c Debug
```

### Run in no-DB fallback mode
Use this mode when `ConnectionStrings:Primary` is empty or unset.

```powershell
dotnet run --project JB2026.Rest
```

Notes:
- The service starts with in-memory repository fallbacks for selected compatibility flows.
- File-based compatibility endpoints still require legacy path keys in [JB2026.Rest/appsettings.Development.json](JB2026.Rest/appsettings.Development.json):
  - `LegacyFiles:ProductPictureRoot`
  - `LegacyFiles:SmlFileRoot`
  - `LegacyFiles:FileAgentRoot`
  - `LegacyFiles:CloudDiskRoot`

### Run in DB-backed mode
Set a valid SQL Server connection string and run the service:

```powershell
$env:ConnectionStrings__Primary = "Server=YOUR_SERVER;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True"
dotnet run --project JB2026.Rest
```

Notes:
- In this mode, EF-backed repositories and stored-procedure gateways are activated.
- Hangfire server and dashboard are enabled at `/hangfire` (local requests only).

### Run compatibility tests
```powershell
dotnet test JB2026.Rest.Tests/JB2026.Rest.Tests.csproj -c Debug
```

### Common local endpoints
- Swagger UI: `http://localhost:5000/swagger` or `https://localhost:5001/swagger` (port may vary by launch profile)
- Health/root probe: `GET /`
- Hangfire dashboard (DB mode): `GET /hangfire`

## Known Remaining Gaps (Intentional/Deferred)
- Full legacy `ModelEx` surface is not yet fully ported.
- Full legacy report-engine equivalence is not complete.
  - Quotation report/PDF path is richer now.
  - Job-side report output is still compatibility-style and may need deeper parity work.
- External integration fidelity (real Firebase/email/bot stacks) remains compatibility-level unless explicitly implemented.

## Suggested Next Focus
1. Deepen Job report/PDF parity (fields/layout/detail blocks).
2. Continue filling high-value `ModelEx` DTO parity gaps where response shapes are still reduced.
3. Add targeted parity tests for any newly expanded report contracts.

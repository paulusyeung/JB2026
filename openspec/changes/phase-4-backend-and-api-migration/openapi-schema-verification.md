# OpenAPI Schema Verification

## Date

- 2026-03-27

## Objective

- Verify OpenAPI generation is enabled for `JB2026.Api` and `JB2026.Rest`.
- Confirm generated schema for migrated API slices reflects the v2 contract after route retirement.

## Build Validation

- `dotnet build .\JB2026.Api\JB2026.Api.csproj -c Release` -> success
- `dotnet build .\JB2026.Rest\JB2026.Rest.csproj -c Release` -> success

## Schema Capture

- Started API host on `http://localhost:8001` and captured:
  - `contracts/JB2026.Api.swagger.v1.json`
- Started REST host on `http://localhost:8002` and captured:
  - `contracts/JB2026.Rest.swagger.v1.json`

## API Schema Route Verification (`JB2026.Api.swagger.v1.json`)

Verified path keys include all migrated v2 slices:

- `/api/v2/auth/token`
- `/api/v2/auth/token/{username}/{password}`
- `/api/v2/user-profiles/me`
- `/api/v2/user-profiles/{username}`
- `/api/v2/jobs/range`
- `/api/v2/jobs/{id}`
- `/api/v2/jobs/{id}/details`
- `/api/v2/job-orders`
- `/api/v2/job-orders/{id}`
- `/api/v2/quotations`
- `/api/v2/quotations/search/{keyword}`
- `/api/v2/quotations/{id}/pdf`

## REST Schema Route Verification (`JB2026.Rest.swagger.v1.json`)

- Verified minimal host contract path:
  - `/`

## Conclusion

- Task 4.1 complete: OpenAPI/Swagger generation is enabled for both hosts.
- Task 4.2 complete: generated API schema reflects migrated v2 endpoints.
- Task 4.3 complete: consumer migration guide is published at `docs/phase-4-backend-and-api-migration/phase-4-api-migration-guide.md`.
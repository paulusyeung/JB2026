# Phase 2 API Pilot Blueprint

## Objective
Provide a canonical migration pattern for moving medium-complex legacy Web API behavior to ASP.NET Core.

## Legacy Endpoint Reference
- Legacy controller used for behavior shape: `C:/Projects/JB2015/JB5.REST/Controllers/JobController.cs`
- Parity characteristics modeled:
  - date-window range query
  - order projection with combined order/job number
  - authenticated access

## Pilot Implementation
- API project: `spikes/phase-2/JB2026.ApiPilot`
- Versioned routes:
  - `POST /api/v1/auth/token`
  - `GET /api/v1/jobs/range?startOn=yyyy-MM-dd&days=N`
  - `GET /api/v1/jobs/{id}`
- Contracts:
  - `JobListItem`
  - `JobDetail`
- Auth model:
  - JWT bearer authentication
  - role claim included in token payload

## Parity Test Blueprint
- Test project: `spikes/phase-2/JB2026.ApiPilot.Tests`
- Baseline snapshots:
  - `Baselines/jobs-range.json`
  - `Baselines/job-detail.json`
- Assertions:
  - unauthorized without bearer token
  - range payload strict-equivalent to baseline snapshot
  - detail payload strict-equivalent to baseline snapshot

## Security Baseline in Pilot
- Auth: required on jobs endpoints
- Input validation: days range constrained to 1-31
- CORS: controlled by named policy for Vue host
- Error handling: explicit non-success guards in client and tests

## How to Reuse in Future Slices
1. Introduce new versioned controller under `/api/v1/{domain}`.
2. Keep DTOs explicit and snapshot baseline under test project.
3. Add unauthorized test and contract parity test before expanding endpoint behavior.
4. Keep migration notes in per-slice blueprint update section.
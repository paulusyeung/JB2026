# Phase 4 Parity Test Progress

## Date

- 2026-03-27

## Scope Completed

- Created solution-level integration test project for parity validation:
  - `JB2026.Api.ParityTests`
- Wired generated legacy snapshots from `openspec/changes/phase-4-backend-and-api-migration/snapshots/` into test output.
- Added quotation parity coverage:
  - Search mapping: legacy `rest_quotation_keyword.json` -> migrated `/api/v2/quotations/search/{keyword}`
  - Range mapping: legacy `rest_quotation_range.json` -> migrated `/api/v2/quotations?startOn={date}&days={n}`
- Added jobs parity coverage:
  - Range mapping: legacy `rest_job_range.json` -> migrated `/api/v2/jobs/range?startOn={date}&days={n}`
  - By-month surrogate mapping: legacy `rest_job_by_month_all.json` -> migrated `/api/v2/jobs/range?startOn={date}&days=31`
  - Job orders mapping: legacy `api_joborders_list.json` -> migrated `/api/v2/job-orders`

## Normalization Rules Implemented

- If legacy snapshot `success=true`:
  - Assert status-code equality.
  - Normalize payload container (`array` vs object with `value` array).
  - Assert count parity and shared property-set compatibility on first item.
- If legacy snapshot `success=false` (known environment instability, e.g., legacy 500):
  - Assert migrated endpoint does not regress into 5xx.
  - Assert migrated payload remains valid JSON array/object.

## Test Validation

- Command:
  - `dotnet test .\JB2026.Api.ParityTests\JB2026.Api.ParityTests.csproj -c Release`
- Result:
  - Total: 5
  - Passed: 5
  - Failed: 0
  - Skipped: 0

## CI Gate Integration (Task 3.5)

- Added explicit parity test step to `.github/workflows/ci.yml`:
  - `dotnet test JB2026.Api.ParityTests/JB2026.Api.ParityTests.csproj --configuration Release --no-build --verbosity normal`
- Updated CI validation documentation in `docs/phase-3-foundation-setup-and-transition-design/ci-pipeline-validation.md` to include parity gate in both blocking gates and local end-to-end validation steps.

## Pre-Prod Deployment Readiness (Task 3.6)

- Added deployment runbook: `preprod-coexistence-deployment-runbook.md`
- Added fill-in execution record template: `preprod-coexistence-execution-record.template.md`
- Added coexistence smoke script: `scripts/verify-coexistence-slice.ps1`
- Completed local rehearsal against `http://localhost:8000`:
  - Attempted: 4
  - Passed: 4
  - Failed: 0
- Rehearsal output captured in `preprod-coexistence-verification.json`
- User confirmed Task 3.6 completion after pre-prod execution.

## UAT Sign-Off Preparation (Task 3.7)

- Added UAT guidance packet: `uat-signoff-packet.md`
- Added fill-in acceptance record: `uat-signoff-record.template.md`
- Added route mapping reference: `uat-route-matrix.md`
- Added focus/deviation guide: `uat-known-deviations-and-focus.md`
- User confirmed Task 3.7 completion with product owner acceptance.

## Auth Compatibility Fix Evidence

- Resolved `405 Method Not Allowed` for legacy token callers by supporting GET token routes.
- Resolved `415 Unsupported Media Type` for POST token callers by tolerating JSON, form, query-string, and header credential input.
- Verified successful token issuance response for `admin` test identity after the fix.
- Confirmed by user that token issuance now works on both `/api/v1/auth/token` and `/api/v2/auth/token`.

## Route Retirement (Task 3.8)

- Disabled legacy `v1` route prefixes for migrated slices (auth, user profiles, jobs, job orders, quotations).
- Updated parity tests and migration documentation to use `v2` routes as the supported contract.

## Phase 4 Quality Gate Closure (Section 5)

- 5.1 validated with code scan: zero `HttpContext.Current` matches in source/project files.
- 5.2 revalidated parity suite with current result: 5 total, 0 failed, 5 passed.
- 5.3 validated with project metadata scan: zero `Owin`, `Katana`, `Thinktecture` package references.
- 5.4 confirmed based on recorded UAT acceptance for Task 3.7 and pre-prod/UAT evidence pack.
- Full evidence captured in `phase-4-quality-gate-verification.md`.

## Notes

- This completes Task 3.4 baseline wiring and execution for currently migrated quotation/jobs slices.
- Task 3.5 is now wired through explicit CI parity-test execution.

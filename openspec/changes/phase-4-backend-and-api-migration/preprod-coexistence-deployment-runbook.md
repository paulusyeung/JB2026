# Pre-Prod Coexistence Deployment Runbook (Task 3.6)

## Objective

Deploy migrated Phase 4 slices to pre-prod while legacy coexistence routes remain available. Current migrated slices in scope:

- Authentication (`/api/v1/auth/*`, `/api/v2/auth/*`)
- Jobs (`/api/v1/jobs/*`, `/api/v2/jobs/*`)
- Job Orders (`/api/v1/job-orders/*`, `/api/v2/job-orders/*`)
- Quotations (`/api/v1/quotations/*`, `/api/v2/quotations/*`)

## Preconditions

- CI pipeline green, including explicit parity gate:
  - `.github/workflows/ci.yml` step `Parity Tests (Phase 4)`
- Release artifact built from mainline commit.
- Legacy pre-prod APIs reachable (JB5.API and JB5.REST).
- Pre-prod secrets prepared for JWT key, issuers, and connection strings.
- Rollback owner and deployment approver identified.

## Configuration Checklist (Pre-Prod)

- `ASPNETCORE_ENVIRONMENT=Staging`
- `Jwt:Key` set via secret store (not from source)
- `Jwt:Issuer` and `Jwt:Audience` aligned with pre-prod clients
- `Cors:AllowedOrigins` set to pre-prod UI origins
- `JB2026:Environment:DeploymentRing=PreProd`
- Observability endpoint configured (`JB2026:Observability:OtlpEndpoint`)

## Deployment Procedure

1. Deploy `JB2026.Api` artifact to pre-prod slot/environment.
2. Keep legacy JB2015 endpoints online during this release window.
3. Apply configuration values and restart application.
4. Validate health endpoint (`/healthz` or service root) for startup readiness.
5. Execute coexistence smoke checks:
   - `scripts/verify-coexistence-slice.ps1 -BaseUrl <preprod-jb2026-api-url>`
6. Store generated verification report as deployment evidence:
   - `preprod-coexistence-verification.json`
7. Announce slice availability for UAT with both v1 and v2 routes active.

## Verification Criteria

- Token issuance succeeds on both route families:
  - `/api/v1/auth/token`
  - `/api/v2/auth/token`
- Endpoint status parity for migrated slices between v1 and v2 routes.
- Collection count parity for range/list endpoints checked by the smoke script.
- No 5xx responses during smoke checks.

## Rollback Procedure

1. If smoke checks fail with migration regression, route traffic back to legacy for impacted consumers.
2. Redeploy previous known-good JB2026.Api artifact (or disable migrated route exposure as per gateway rules).
3. Confirm legacy path stability.
4. Open incident record and attach failed verification JSON.

## Evidence to Attach

- CI run link with parity test pass
- Deployment change ticket ID
- `preprod-coexistence-verification.json`
- Incident/rollback notes (if rollback executed)

## Execution Checklist Template

Use this section during the actual pre-prod deployment window.

### Change Control

- Change ticket ID: `________________`
- Deployment approver: `________________`
- Deployment operator: `________________`
- Planned start (UTC): `________________`
- Planned end (UTC): `________________`

### Environment Confirmation

- [ ] Pre-prod URL confirmed: `________________`
- [ ] Legacy JB5.API reachable from pre-prod routing layer
- [ ] Legacy JB5.REST reachable from pre-prod routing layer
- [ ] `ASPNETCORE_ENVIRONMENT=Staging` applied
- [ ] JWT secret injected from secret store
- [ ] CORS origins updated for pre-prod UI
- [ ] Observability exporter target configured

### Execution Log

- Artifact version / commit SHA: `________________`
- Deployment started at (UTC): `________________`
- Application restart completed at (UTC): `________________`
- Health check result: `________________`
- Verification script command used: `powershell -ExecutionPolicy Bypass -File .\openspec\changes\phase-4-backend-and-api-migration\scripts\verify-coexistence-slice.ps1 -BaseUrl __________________`
- Verification output file attached: `________________`

### Acceptance Criteria

- [ ] `/api/v1/auth/token` returns success
- [ ] `/api/v2/auth/token` returns success
- [ ] Quotation search parity verified
- [ ] Quotation range parity verified
- [ ] Jobs range parity verified
- [ ] Job orders list parity verified
- [ ] Verification JSON shows `failed = 0`
- [ ] No 5xx responses observed in logs during smoke window

### Rollback Record

- Rollback required: `Yes / No`
- If yes, rollback start (UTC): `________________`
- If yes, rollback complete (UTC): `________________`
- Incident / notes: `________________`

### Sign-Off

- Technical sign-off name: `________________`
- Product / business witness: `________________`
- Final status: `Ready for Task 3.7 / Hold`
- Signed at (UTC): `________________`

## Status

- Runbook and verification automation prepared in repository.
- Local rehearsal completed successfully using `http://localhost:8000` with the verification script:
  - Attempted: 4
  - Passed: 4
  - Failed: 0
- Rehearsal evidence file: `preprod-coexistence-verification.json`
- Manual execution in actual pre-prod environment is still required for final Task 3.6 sign-off.

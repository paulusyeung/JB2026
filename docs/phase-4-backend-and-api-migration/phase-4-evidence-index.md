# Phase 4 Evidence Index

## OpenSpec Change Root
- `openspec/changes/phase-4-backend-and-api-migration/`

## Planning And Design
- `proposal.md` — scope, rationale, and intended capability changes
- `design.md` — migration decisions, risks, and phased execution model
- `legacy-endpoint-inventory.md` — initial legacy API catalog
- `endpoint-prioritization.md` — migration ordering by business criticality and dependency depth
- `coexistence-routing-design.md` — coexistence route model and cutover rules

## Snapshot And Parity Evidence
- `snapshot-collection-strategy.md` — capture strategy and execution notes
- `snapshots/snapshot-metadata.json` — capture metadata
- `snapshots/snapshot-summary.json` — endpoint-by-endpoint capture result summary
- `parity-test-progress.md` — implementation log for parity, CI, pre-prod readiness, and UAT prep

## Pre-Prod Deployment Evidence Assets
- `preprod-coexistence-deployment-runbook.md` — deployment and rollback runbook
- `preprod-coexistence-execution-record.template.md` — fill-in deployment record for real pre-prod run
- `preprod-coexistence-verification.json` — local rehearsal output from coexistence smoke verification
- `scripts/verify-coexistence-slice.ps1` — coexistence verification automation

## UAT Evidence Assets
- `uat-signoff-packet.md` — UAT guidance and acceptance criteria
- `uat-signoff-record.template.md` — fill-in sign-off record
- `uat-route-matrix.md` — route mapping for v1/v2 validation
- `uat-known-deviations-and-focus.md` — intentional differences and business focus areas

## API Migration Documentation
- `docs/phase-4-backend-and-api-migration/phase-4-api-migration-guide.md` — route retirement summary and v2 contract guidance

## API Contract Evidence
- `openspec/changes/phase-4-backend-and-api-migration/contracts/JB2026.Api.swagger.v1.json` — generated API host OpenAPI schema
- `openspec/changes/phase-4-backend-and-api-migration/contracts/JB2026.Rest.swagger.v1.json` — generated REST host OpenAPI schema
- `openspec/changes/phase-4-backend-and-api-migration/openapi-schema-verification.md` — schema verification execution and migrated route coverage

## Quality Gate Evidence
- `openspec/changes/phase-4-backend-and-api-migration/phase-4-quality-gate-verification.md` — verification outputs for tasks 5.1 to 5.4

## Code Deliverables
- `JB2026.Api/Controllers/AuthController.cs`
- `JB2026.Api/Controllers/UserProfilesController.cs`
- `JB2026.Api/Controllers/JobsController.cs`
- `JB2026.Api/Controllers/JobOrdersController.cs`
- `JB2026.Api/Controllers/QuotationsController.cs`
- `JB2026.Api/Program.cs`
- `JB2026.Infrastructure/Extensions/Jb2026ServiceCollectionExtensions.cs`
- `JB2026.Infrastructure/Extensions/Jb2026ApplicationBuilderExtensions.cs`
- `JB2026.Api.ParityTests/QuotationParityTests.cs`
- `JB2026.Api.ParityTests/JobsParityTests.cs`
- `.github/workflows/ci.yml`

## Current Decision Boundary
- Task 5 quality gate confirmation is complete through 5.4.
- Phase 4 deliverables are complete and ready for downstream planning transition.

# Phase 4 API Migration Guide

## Purpose

This guide documents the route changes applied in Phase 4 after slice cutover, including legacy route retirement and the supported endpoint contract for consumers.

## Version Policy

- Legacy `v1` routes for migrated Phase 4 slices are now retired.
- Supported routes for migrated slices are `v2` only.
- Any remaining legacy domains not yet migrated continue under their existing legacy routing until their slice is cut over.

## Route Changes

### Authentication

- Retired:
  - `POST /api/v1/auth/token`
  - `GET /api/v1/auth/token`
  - `GET /api/v1/auth/token/{username}/{password}`
- Supported:
  - `POST /api/v2/auth/token`
  - `GET /api/v2/auth/token`
  - `GET /api/v2/auth/token/{username}/{password}`

### User Profiles

- Retired:
  - `GET /api/v1/user-profiles/me`
  - `GET /api/v1/user-profiles/{username}`
- Supported:
  - `GET /api/v2/user-profiles/me`
  - `GET /api/v2/user-profiles/{username}`

### Jobs

- Retired:
  - `GET /api/v1/jobs/range`
  - `GET /api/v1/jobs/{id}`
  - `GET /api/v1/jobs/{id}/details`
- Supported:
  - `GET /api/v2/jobs/range`
  - `GET /api/v2/jobs/{id}`
  - `GET /api/v2/jobs/{id}/details`

### Job Orders

- Retired:
  - `GET /api/v1/job-orders`
  - `GET /api/v1/job-orders/{id}`
  - `POST /api/v1/job-orders`
  - `PUT /api/v1/job-orders/{id}`
  - `DELETE /api/v1/job-orders/{id}`
- Supported:
  - `GET /api/v2/job-orders`
  - `GET /api/v2/job-orders/{id}`
  - `POST /api/v2/job-orders`
  - `PUT /api/v2/job-orders/{id}`
  - `DELETE /api/v2/job-orders/{id}`

### Quotations

- Retired:
  - `GET /api/v1/quotations`
  - `GET /api/v1/quotations/search/{keyword}`
  - `GET /api/v1/quotations/{id}/pdf`
- Supported:
  - `GET /api/v2/quotations`
  - `GET /api/v2/quotations/search/{keyword}`
  - `GET /api/v2/quotations/{id}/pdf`

## Client Migration Checklist

- Update all hardcoded `v1` route references to `v2`.
- Verify token issuance calls target `/api/v2/auth/token`.
- Re-run integration tests against the `v2` contract.
- Confirm error handling still accepts structured ProblemDetails payloads.

## Deprecation Timeline

- Phase 4 cutover date: 2026-03-27
- `v1` endpoints for migrated slices retired immediately after UAT confirmation.
- Any consumer still using retired `v1` routes must migrate before Phase 5 gate review.

## References

- `docs/phase-4-backend-and-api-migration/phase-4-review-summary.md`
- `openspec/changes/phase-4-backend-and-api-migration/tasks.md`
- `openspec/changes/phase-4-backend-and-api-migration/parity-test-progress.md`

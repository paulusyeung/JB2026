# Product Owner UAT Sign-Off Packet (Task 3.7)

## Objective

Capture product owner acceptance for the migrated Phase 4 API slices after successful pre-prod coexistence deployment.

Slices in scope:

- Authentication
- Jobs
- Job Orders
- Quotations

## Entry Criteria

All of the following must exist before UAT starts:

- Task 3.6 evidence attached from actual pre-prod execution
- CI parity gate green
- Pre-prod coexistence verification JSON attached
- No open P1/P2 defects for the migrated slices
- Named product owner and technical witness assigned

## Required Evidence Inputs

- Pre-prod deployment record
- `preprod-coexistence-verification.json`
- CI parity test pass reference
- Snapshot parity documentation in `parity-test-progress.md`
- Known deviations list, if any
- UAT route matrix (`uat-route-matrix.md`)
- UAT focus/deviation guide (`uat-known-deviations-and-focus.md`)

## UAT Scenarios

### 1. Authentication

- Request token via `/api/v1/auth/token`
- Request token via `/api/v2/auth/token`
- Confirm both routes authenticate approved test user successfully
- Confirm invalid credentials are rejected with expected error response

### 2. Jobs

- Query jobs range on v1 route
- Query jobs range on v2 route
- Confirm returned list is usable for business workflow review
- Confirm a representative job detail can be opened/read successfully

### 3. Job Orders

- Open job orders list on v1 route
- Open job orders list on v2 route
- Confirm representative order data is visible and complete
- If operationally safe in pre-prod, create/update/delete one representative order and confirm expected behavior

### 4. Quotations

- Search quotations on v1 route
- Search quotations on v2 route
- Run quotations date-range query on v1 route
- Run quotations date-range query on v2 route
- Confirm a representative quotation PDF route behaves as expected if available in pre-prod test data

## Acceptance Questions

The product owner should explicitly answer:

- Does the migrated slice satisfy the expected business workflow for jobs, job orders, and quotations?
- Are any differences from legacy acceptable for this phase?
- Is the slice approved to remain active in pre-prod for further rollout progression?
- Are there any blockers before legacy route retirement planning begins?

## Exit Criteria

Task 3.7 can be considered complete only when:

- Product owner name is recorded
- UAT execution date/time is recorded
- Outcome is marked `Accepted` or `Accepted with Conditions`
- Any conditions or defects are listed explicitly
- Technical witness confirms the tested build/version

## Known Constraints

- UAT sign-off does not authorize legacy route retirement by itself.
- Task 3.8 still requires documented route retirement planning and API documentation updates.
- If UAT finds blocking issues, reopen the slice checklist rather than recording partial acceptance as full sign-off.

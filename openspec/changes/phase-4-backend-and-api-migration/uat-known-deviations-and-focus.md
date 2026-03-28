# UAT Known Deviations And Focus Areas

## Purpose

This guide helps the product owner focus on business-significant behavior rather than implementation-level route differences introduced during migration.

## Expected Differences From Legacy

### Authentication Contract

- Legacy token generation used GET routes and sometimes header-based credentials.
- Migrated API supports `POST /api/v1/auth/token` and `POST /api/v2/auth/token` using JSON body, and also accepts form/query/header credential input for compatibility.
- Legacy-compatible GET token routes are also available for coexistence testing.
- UAT should validate successful authentication behavior, not legacy transport mechanics.

### Route Naming

- Migrated APIs expose normalized pluralized routes such as `/jobs`, `/job-orders`, and `/quotations`.
- Both `/api/v1/*` and `/api/v2/*` remain available for coexistence/UAT during this phase.
- UAT should focus on workflow correctness and data usability across both route families.

### Error Shape

- Legacy APIs often returned null or inconsistent error bodies.
- Migrated APIs return structured `ProblemDetails` / validation problem responses.
- This is an intentional modernization and should be treated as acceptable unless a client contract depends on the legacy error payload.

### Seeded Test Data

- Current migrated API uses seeded in-memory data for jobs, job orders, quotations, and configured identities in the local/dev path.
- UAT should validate business flow and field usefulness, not production-volume realism.

## Business Focus Areas

### Authentication

- Can approved users authenticate reliably?
- Is invalid login rejection understandable and safe?

### Jobs

- Are range results usable for operational review?
- Are job details and style-title lookups understandable to the business user?

### Job Orders

- Is the order list intelligible and complete enough for current workflow needs?
- Are create/update/delete behaviors acceptable in pre-prod test conditions if exercised?

### Quotations

- Can users search and find expected quotation records?
- Is date-range behavior acceptable?
- Does the PDF route provide the expected downloadable/previewable output behavior?

## UAT Blocking Conditions

Do not sign off if any of the following occur:

- A migrated route returns 5xx during normal workflow checks
- Token issuance fails for approved test users
- Core list/detail responses are missing business-critical fields
- A difference from legacy breaks an agreed user workflow
- PDF or detail retrieval fails for representative records needed by the workflow

## Acceptable Conditional Sign-Off Examples

Conditional sign-off is acceptable only if:

- The issue is cosmetic or non-blocking to business workflow
- A follow-up task is explicitly recorded
- The product owner states that rollout can continue with the condition documented

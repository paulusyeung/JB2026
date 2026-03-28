# Design — phase-7-cutover-readiness-support-and-enablement

## Context

By the start of Phase 7, all API, data, and UI slices have been delivered and feature-flag-toggled in staging. The legacy JB2015 system is still running. Phase 7 validates that JB2026 is production-ready, that support ownership is in place, and that the team can safely execute and if necessary reverse the cutover.

## Goals

- All regression tests green across API, data, and UI layers
- p95 API latency ≤ 500 ms under 2× expected peak concurrent users
- DR drill completed successfully in staging: full application restore within the agreed RTO
- Rollback runbook executed successfully: traffic returned to JB2015 within 15 minutes
- Zero critical/high OWASP Top 10 findings
- Training, support ownership, and escalation readiness completed before cutover approval
- Go/no-go checklist completed and signed by technical lead and product owner

## Non-Goals

- Introducing new features (strictly hardening and validation only)
- Migrating additional API or UI slices beyond what was delivered in Phases 4–6
- Modifying the legacy JB2015 system

## Decisions

### D1: k6 for Load Testing
k6 (GNU AGPL v3 / free OSS) is used for load test scripts. Scripts are checked into the repository and run from CI on a dedicated agent.

### D2: OWASP Dependency-Check for Vulnerability Scanning
`dependency-check` (Apache 2.0) scans NuGet and NPM packages against the NVD CVE database. Blocking threshold: CVSS ≥ 7.0.

### D3: Semgrep for SAST
Semgrep (community rules, LGPL) is added to CI for static analysis. Custom rules target SQL injection, secrets in source, and insecure deserialization patterns.

### D4: Pre-Production Rehearsal Is Mandatory
The cutover runbook must be executed in full in a production-equivalent staging environment before any go/no-go meeting. Time-to-restore and time-to-rollback are recorded.

### D5: Go/No-Go Criteria Are Hard Gates
Any failed gate (regression, load, DR, security) must be resolved and re-validated before a go/no-go meeting can be called.

### D6: Support Readiness Requires Explicit Ownership Acceptance
Training completion alone is insufficient. Engineering, operations, and support owners must explicitly accept their roles, escalation paths, and hypercare responsibilities before cutover approval.

## Risks

| ID | Risk | Mitigation |
|----|------|------------|
| H-R1 | Performance regression found late | Load tests run from earlier phases in CI; Phase 7 formalises prod-representative scenarios |
| H-R2 | Rollback takes longer than 15 minutes | Rollback steps automated in scripts; timed dry run in Phase 7 |
| H-R3 | Security scan reveals high-severity CVE in a dependency | Dependency-check runs in CI from earlier phases; known issues triaged before Phase 7 starts |
| H-R4 | Staging is not representative of production | Staging environment uses production DB snapshot (anonymised) and production-equivalent infrastructure |
| H-R5 | Support team is unprepared for post-cutover incidents | Training and operational handoff are completed and signed off before go/no-go |

## Timeline

| Week | Activity |
|------|----------|
| 20–21 | Full regression run; triage and fix any failures |
| 21–22 | Load tests: baseline, peak, soak scenarios |
| 22   | DR drill (simulate DB failure, app node failure); measure RTO |
| 22   | Rollback drill (simulate failed cutover); measure time-to-rollback |
| 22   | Security sweep: OWASP Dependency-Check, Semgrep SAST, secrets scan |
| 22   | Training, support handoff, and documentation refresh |
| 22   | Pre-production cutover rehearsal end-to-end |
| 22   | Go/no-go meeting: sign-off checklist |

Support readiness acceptance criteria:
- Named owners exist for engineering, operations, and first-line support during hypercare.
- Escalation path is documented with contact method and response expectations.
- Final runbooks and support references are published and reviewed.
- Knowledge-transfer sessions are completed with attendance recorded.

## Open Questions

- Q1: What is the agreed production RTO for Phase 8? (Operations to confirm)
- Q2: Is a third-party penetration test required before go-live? (Risk/Compliance to confirm)

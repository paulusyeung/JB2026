# Tasks — phase-7-cutover-readiness-support-and-enablement

## Group 1: Full Regression Suite

- [ ] Consolidate all unit, integration, and Playwright E2E tests into a single CI pipeline stage
- [ ] Build coverage map: migrated API routes vs. existing regression tests
- [ ] Write missing regression scenarios for any uncovered migrated routes
- [ ] Run full regression suite against staging; triage and fix any failures
- [ ] Confirm suite exit code = 0 with all tests passing

## Group 2: Load and Performance Tests

- [ ] Document expected production peak concurrent user count (Operations input)
- [ ] Write k6 peak load scenario (2× expected concurrent users)
- [ ] Write k6 soak scenario (expected peak load for 30 minutes)
- [ ] Run baseline, peak, and soak scenarios; capture results artefact
- [ ] Confirm p95 latency ≤ 500 ms and error rate < 1% for all critical endpoints
- [ ] Investigate and resolve any performance regressions found
- [ ] Commit k6 scripts to repository under `tests/load/`

## Group 3: DR and Rollback Drills

- [ ] Promote staging to production-equivalent configuration (DB snapshot, infra parity)
- [ ] Write rollback runbook: steps to return traffic to JB2015 within 15 minutes
- [ ] Write DR runbook: steps to restore from database backup within agreed RTO
- [ ] Commit both runbooks to `docs/runbooks/`
- [ ] Execute rollback drill in staging; record actual time-to-rollback
- [ ] Execute DR drill in staging; record actual time-to-restore
- [ ] Confirm both drills meet their time targets; re-drill if either fails

## Group 4: Security Hardening

- [ ] Run OWASP Dependency-Check; triage and remediate CVSS ≥ 7.0 findings
- [ ] Run Semgrep SAST; triage and remediate critical/high findings
- [ ] Run secrets scan (truffleHog or gitleaks) across full git history; rotate any found secrets
- [ ] Verify all production endpoints enforce HTTPS and return HSTS headers
- [ ] Review authentication and session management: token expiry, cookie flags, CSRF protection
- [ ] Add OWASP Dependency-Check and Semgrep to CI as blocking gates

## Group 5: Pre-Production Cutover Rehearsal

- [ ] Execute full cutover runbook end-to-end in staging (without rolling back)
- [ ] Validate all smoke tests pass after simulated cutover in staging
- [ ] Record time-to-complete and any issues encountered
- [ ] Update cutover runbook with lessons from rehearsal

## Group 6: Go/No-Go Checklist

- [ ] Full regression suite: 100% pass
- [ ] p95 load test: ≤ 500 ms at 2× peak, soak test stable
- [ ] Rollback drill: time-to-rollback ≤ 15 minutes
- [ ] DR drill: time-to-restore within agreed RTO
- [ ] Security: zero CVSS ≥ 7.0 dependency vulnerabilities, zero SAST critical/high, zero secrets
- [ ] HTTPS + HSTS enforced on all endpoints
- [ ] Cutover rehearsal completed in staging
- [ ] Runbooks committed and reviewed
- [ ] Go/no-go meeting held; technical lead and product owner signature on checklist

## Group 7: Support Enablement and Knowledge Transfer

- [ ] Finalize training materials for engineering, operations, and support teams
- [ ] Conduct knowledge-transfer sessions for on-call, support, and release owners
- [ ] Review and approve operational support readiness, escalation procedures, and ownership handoffs
- [ ] Update migration documentation set with final architecture, runbooks, and support references
- [ ] Confirm support handoff is complete before production cutover approval
- [ ] Record named hypercare owners for engineering, operations, and support
- [ ] Record attendance and acceptance for knowledge-transfer sessions

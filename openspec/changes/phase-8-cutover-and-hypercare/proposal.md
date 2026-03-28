# Proposal — phase-8-cutover-and-hypercare

## Why

The go/no-go checklist from Phase 7 is signed. All API, data, and UI slices have been validated in staging. Phase 8 executes the production switchover, monitors the new system under live traffic, and decommissions the legacy JB2015 application once stability is confirmed.

## What Changes

Production traffic is moved from JB2015 to JB2026 using a canary or blue-green deployment strategy. Hypercare monitoring runs for a defined post-cutover period. Once the system is confirmed stable and the hypercare period closes, the legacy JB2015 application, coexistence routing shims, and feature flag infrastructure are decommissioned.

## Capabilities

- `production-cutover` — controlled traffic switch from JB2015 to JB2026 in production using a canary ramp or blue-green flip, with immediate rollback capability retained throughout
- `hypercare-monitoring` — elevated on-call monitoring, error budget tracking, and daily health check reviews for the hypercare period following cutover
- `legacy-decommission` — retirement of the JB2015 application, infrastructure, coexistence routing layer, and feature flags once stability is confirmed

## Impact

- **Users** — Experience the production JB2026 application; support team is on elevated alert for the hypercare period
- **Operations** — Runbook executed live; monitoring dashboards promoted to production alerting; legacy infrastructure deprovisioned post-hypercare
- **Project** — Completion of the full JB2015 → JB2026 migration; open-source publication ready

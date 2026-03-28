# Design — phase-8-cutover-and-hypercare

## Context

All JB2026 slices are running in staging with feature flags enabled. The Phase 7 go/no-go checklist is signed. JB2015 is still serving 100% of production traffic. Phase 8 transitions production traffic to JB2026, operates a hypercare period, and retires JB2015.

## Goals

- Execute production cutover with zero data loss
- Achieve ≥ 99.9% uptime during the cutover window
- Complete hypercare period (minimum 2 weeks) with error rate within SLA
- Decommission JB2015 and all coexistence infrastructure cleanly

## Non-Goals

- Introducing new features (any post-go-live feature work is a new project increment)
- Migrating Google GData (explicitly out of scope throughout)
- Changing the data schema during or after cutover

## Decisions

### D1: Blue-Green Deployment Strategy
JB2026 is kept live at its staging slot (blue). The production slot (green) is JB2015. Cutover is a DNS/load-balancer flip to the JB2026 slot. Rollback is a flip back within minutes.

### D2: Canary Ramp Optional Pre-Flip
If the load balancer supports weighted routing, a 10% → 50% → 100% canary ramp over 2 hours is preferred over an instant flip. If not available, a single DNS flip is acceptable given Phase 7 validation.

### D3: Hypercare Period = 2 Weeks Minimum
Elevated on-call coverage (≤ 15 min response SLA) is maintained for a minimum of 2 weeks post-cutover. Exit from hypercare requires 5 consecutive business days with no P1/P2 incidents.

### D4: JB2015 Decommission Is Gated on Hypercare Exit
JB2015 application servers and coexistence routing infrastructure are NOT deprovisioned until hypercare is formally closed by the technical lead.

### D5: Open-Source Publication After Decommission
The JB2026 repository is prepared for public GitHub publication (licence file, CONTRIBUTING.md, security policy, CI badge) only after JB2015 is fully decommissioned.

## Risks

| ID | Risk | Mitigation |
|----|------|------------|
| C-R1 | Data written to JB2026 during canary ramp cannot sync to JB2015 for rollback | Cutover is a read-only window (maintenance mode) during DNS flip; write traffic resumes only after full flip is confirmed |
| C-R2 | Unexpected production-only configuration issue surfaces after flip | Hypercare on-call escalation path defined; rollback script maintained and tested |
| C-R3 | Legacy JB2015 prematurely deprovisioned before hypercare closes | Decommission task is behind a manual gate requiring technical lead sign-off |
| C-R4 | Open-source publication exposes secrets in git history | Secrets scan (from Phase 7) confirms clean history before publication |

## Cutover Sequence

1. Enter maintenance window; show maintenance page to users
2. Take final JB2015 database backup
3. Confirm JB2026 is healthy in its current slot (smoke tests)
4. Flip load balancer / DNS to JB2026 (or start canary ramp)
5. Run post-flip smoke tests in production
6. Confirm error rate and latency metrics normal in monitoring dashboard
7. Exit maintenance window; users on JB2026
8. Retain JB2015 slot warm for rollback for at least 72 hours

## Open Questions

- Q1: Does the production environment support weighted canary routing? (Ops to confirm)
- Q2: What is the agreed maintenance window duration? (Product Owner/Ops to confirm)
- Q3: Who is the designated approver for hypercare exit sign-off?

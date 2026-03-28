# Tasks — phase-8-cutover-and-hypercare

## Group 1: Cutover Preparation

- [ ] Confirm go/no-go checklist from Phase 7 is signed by technical lead and product owner
- [ ] Confirm JB2015 slot remains warm and rollback-ready
- [ ] Book and communicate maintenance window to all stakeholders
- [ ] Confirm monitoring dashboard is configured for production alerts
- [ ] Brief on-call rotation team; test alerting channels
- [ ] Prepare maintenance page and load-balancer configuration for the flip

## Group 2: Production Cutover Execution

- [ ] Enter maintenance window; activate maintenance page
- [ ] Take final JB2015 database backup; confirm backup integrity
- [ ] Run JB2026 pre-flip smoke tests in its deployment slot; confirm all pass
- [ ] Execute load-balancer/DNS flip to JB2026 (canary ramp if supported: 10% → 50% → 100%)
- [ ] Run post-flip smoke tests against the production endpoint; confirm all pass
- [ ] Confirm error rate and latency metrics are within SLA in monitoring dashboard
- [ ] Exit maintenance window; mark cutover complete in change record
- [ ] Confirm JB2015 slot remains warm for 72 hours

## Group 3: Hypercare Monitoring

- [ ] Activate elevated on-call rota from cutover moment (≤ 15-min ack SLA)
- [ ] Run daily health check review: error rate, p95 latency, active users, open incidents
- [ ] Record all P1/P2 incidents and root-cause findings in incident log
- [ ] Evaluate hypercare exit criteria daily after day 5: 5 consecutive business days with zero P1/P2
- [ ] Hold hypercare exit review with technical lead; obtain sign-off

## Group 4: Legacy Decommission

- [ ] Confirm hypercare exit sign-off is on file before raising decommission tasks
- [ ] Deprovision JB2015 application servers
- [ ] Deprovision JB2015 database read replicas (retain primary backup for defined retention period)
- [ ] Remove coexistence routing middleware from JB2026 codebase
- [ ] Remove feature flag tables and seeding scripts
- [ ] Confirm no feature-flag or coexistence references remain in codebase (grep check)
- [ ] Archive JB2015 source code repository (read-only)
- [ ] Close and decommission any JB2015-specific infrastructure (CI agents, deployment slots)

## Group 5: Open-Source Publication

- [ ] Choose and add OSI-approved `LICENSE` file to repository root (confirm with legal)
- [ ] Write `CONTRIBUTING.md` with contribution guidelines and DCO/CLA note
- [ ] Write `SECURITY.md` with vulnerability disclosure policy and contact
- [ ] Add CI status badge to `README.md`
- [ ] Run final secrets scan across full git history; confirm clean
- [ ] Confirm all proprietary DevExpress and CKEditor references are absent from the repository
- [ ] Set repository visibility to public on GitHub

## Group 6: Project Closure

- [ ] Update `README.md` with final JB2026 architecture, setup, and contribution guide
- [ ] Archive `task.md` as historical reference (rename to `task-completed.md` or move to `docs/`)
- [ ] Issue project closure communication to stakeholders
- [ ] Conduct retrospective; document lessons learned

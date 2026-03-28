# Support Operating Model

## Post-Cutover Ownership
| Area | Primary Owner | Backup Owner |
|---|---|---|
| API runtime and deployments | Engineering (API Lead) | DevOps Lead |
| Platform infrastructure and hosting | Operations Lead | Platform Lead |
| End-user support and incident intake | Support Lead | Operations Lead |
| Data reliability and integrity incidents | Data Lead | API Lead |

## Escalation Path
1. Support triage opens incident and classifies severity.
2. Operations validates platform health and routes to owning engineering lead.
3. Engineering performs mitigation and provides recovery ETA.
4. Sev-1 or Sev-2 incidents escalate to platform and operations leadership within 15 minutes.

## Hypercare Boundaries
- Hypercare duration: first 14 days after cutover.
- Daily incident stand-up during hypercare.
- Exit hypercare when SLA, error rate, and incident volume meet steady-state thresholds for 5 consecutive days.

## Handoff Checkpoints Before Phase 7
- On-call rosters approved and published.
- Runbooks verified by support and operations.
- Escalation contacts tested with dry-run incident.
- Observability dashboards and alerts validated.

# Documentation Gap Register

## Purpose
This register records the remaining information gaps discovered while building the Phase 1 baseline package.

| Gap | Impact | Blocks | Owner Role | Next Action |
|---|---|---|---|---|
| Detailed JB2015 endpoint catalog is not yet documented in this repo | Limits selection precision for API pilot and coexistence planning | Does not block bounded Phase 2 spike planning | API Lead | Extract endpoint inventory from legacy source before Phase 4 slice planning |
| Exact Hangfire job catalog is not yet documented | Limits job-by-job migration sequencing and rollback analysis | Does not block initial spike planning; blocks later operational planning if left unresolved | Data Lead | Inventory recurring and ad hoc jobs from legacy source before Phase 5 execution |
| External integration contract list is not yet documented | Leaves coexistence and partner dependency assumptions at a high level | Does not block initial spikes; raises Phase 4 and Phase 7 risk | API Lead | Capture integration contract matrix alongside API pilot findings |
| Named stakeholders, review dates, and meeting outcomes are absent | Prevents factual completion of formal review and sign-off tasks | Blocks Phase 1 exit approval | Platform Lead | Schedule architecture board and steering review using Phase 0 cadence |
| Current deployment automation references are not linked in this repo | Runbooks are baseline-ready but not yet tied to executable environment assets | Does not block documentation baseline; blocks future operational validation if unresolved | DevOps Lead | Attach environment-specific deployment references during Phase 3 foundation work |
| Rollback timing evidence is not yet recorded | Prevents operational confidence claims beyond minimum runbook presence | Does not block baseline creation; blocks later Gate C readiness evidence | DevOps Lead | Capture rollback rehearsal data during Phase 7 drills |

## Blocking Summary
- Blocking Phase 1 exit approval: named stakeholder review and explicit sign-off evidence.
- Non-blocking for bounded Phase 2 spikes: endpoint detail, per-job detail, and integration contract detail, provided those gaps remain tracked and assigned.
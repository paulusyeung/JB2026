# Migration Decision Log

## Decision Entries

| ID | Date | Decision | Rationale | Source |
|---|---|---|---|---|
| P1-D1 | 2026-03-27 | Phase 2 spikes will use a shared current-state baseline package rather than rediscovering inventory independently. | Prevents conflicting assumptions across UI, API, data, and auth spikes. | Phase 1 OpenSpec design |
| P1-D2 | 2026-03-27 | Dependency rows must include owner and migration disposition before Gate B planning. | Gate B depends on ownership clarity and license strategy, not just package names. | Phase 0 gate criteria and Phase 1 spec |
| P1-D3 | 2026-03-27 | Google GData remains out of scope for JB2026 and must not appear in Phase 2 spike scope. | Scope exclusions must stay explicit to prevent accidental re-entry. | Phase 0 out-of-scope registry and Phase 1 spec |
| P1-D4 | 2026-03-27 | Minimum operational baseline for Phase 1 consists of deployment, rollback, and operational support runbooks. | Later cutover and support phases require these artifacts before technical implementation scales. | Phase 1 design acceptance thresholds |
| P1-D5 | 2026-03-27 | Detailed endpoint and job catalogs are tracked as follow-up gaps and do not block bounded Phase 2 spike planning. | The selected spikes can start with application, dependency, and data-flow coverage already documented. | Phase 1 baseline inventory and gap register |

## Review Recording Rule
All future architecture board or steering decisions for this migration should append a new entry with date, decision, rationale, and artifact link.
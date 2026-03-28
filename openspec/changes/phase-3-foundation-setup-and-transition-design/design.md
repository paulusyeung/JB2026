## Context

Phase 3 follows the risk-spike decisions and provides the engineering and operational foundation required for scaled implementation. The migration task plan requires a buildable .NET 8 solution, shared infrastructure, CI and compliance gates, observability, and explicit transition design outputs before Phase 4 backend implementation begins.

## Goals / Non-Goals

**Goals:**
- Buildable .NET 8 solution scaffold with correct project references and namespaces.
- Shared library providing consistent patterns for config, logging, error handling, and typed API contracts.
- CI pipeline enforcing build, test, lint, security, and license gates before merge.
- Environment configuration and observability baseline ready for downstream implementation.
- Transition playbook, threat model, and support operating model approved before Phase 4 starts.

**Non-Goals:**
- Implement business-domain migration slices.
- Complete production cutover rehearsal or hypercare planning in detail.
- Re-open spike decisions already approved in Phase 2 unless new evidence appears.

## Decisions

1. Engineering foundation is standardized before broad implementation
   - Rationale: Backend, data, and UI teams need one approved baseline for solution structure, shared services, and CI quality gates.

2. Transition mechanics are designed before cutover work begins
   - Rationale: Coexistence, rollback, and ownership handoffs are program-level concerns that should not emerge ad hoc in later phases.

3. Threat modeling is required before implementation scale increases
   - Rationale: Security posture changes materially during migration; risks must be identified before teams multiply attack surface.

4. Support readiness starts in design, not after implementation
   - Rationale: Post-cutover support gaps are expensive if discovered only during hardening.

## Risks / Trade-offs

- [Solution scaffold takes longer than expected if project references are complex] → Mitigation: Keep initial projects as empty shells; add references incrementally per migration slice.
- [License scanner blocks legitimate community edition dependencies] → Mitigation: Maintain an approved-exceptions list reviewed by compliance owner; new exceptions require explicit approval.
- [Divergence if teams start feature work before shared library is stable] → Mitigation: Define shared library API contract first; feature slices consume it read-only until v1 is locked.
- [Transition playbook omits critical rollback dependencies] → Mitigation: Review playbook with platform, API, data, UI, and operations leads before Phase 4 entry.
- [Threat model becomes a document without implementation follow-through] → Mitigation: Convert each high-priority risk into tracked mitigation tasks or explicit accepted-risk decisions.
- [Support ownership is unclear at cutover] → Mitigation: Require named ownership and escalation approval in the support operating model before Phase 7 entry.

## Migration Plan

1. Create solution and project skeletons with shared infrastructure patterns.
2. Stand up CI, configuration, secret handling, and observability baseline.
3. Produce transition playbook, threat model, and support operating model.
4. Review and approve all Phase 3 outputs before Phase 4 starts.

Rollback strategy: Phase 3 outputs are additive. If foundation or transition-design artefacts are incomplete, stop Phase 4 entry, fix the artefacts, and re-run the design review before implementation resumes.

## Open Questions

- Which team owns final approval of the transition playbook: platform lead, operations lead, or joint sign-off?
- Which threat categories require formal sign-off before backend and UI migration scales up?

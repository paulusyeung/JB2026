## Context

Phase 0 establishes execution guardrails before migration implementation. The current project has a migration task plan (`task.md`) that defines phased delivery, open-source dependency replacement requirements, Vue 3 UI direction, and explicit exclusion of Google GData migration. This design formalizes how those planning artifacts become enforceable gates for downstream implementation changes.

## Goals / Non-Goals

**Goals:**
- Define a repeatable governance baseline required before Phase 1 work starts.
- Standardize Gate A/B/C readiness criteria and accountable owners.
- Produce mandatory planning artifacts: migration charter, RACI, dependency matrix, and open-source license compliance matrix.
- Define stakeholder review cadence and benchmarking rules that later phases must follow.
- Enforce explicit scope boundaries, including out-of-scope features.
- Ensure dependency decisions are compliant with open-source redistribution constraints.

**Non-Goals:**
- Implement backend, data, or UI migration code.
- Select final implementation-level libraries for every feature slice.
- Migrate Google GData feature scope.

## Decisions

1. Gate-driven phase control
- Decision: Introduce Gate A/B/C exit criteria as mandatory preconditions for phase transition.
- Rationale: Reduces rework and decision drift by validating architecture, dependency strategy, and cutover readiness at controlled checkpoints.
- Alternative considered: Ad-hoc weekly sign-off without hard gates.
- Why not: Lower traceability and higher risk of unresolved blockers entering implementation.

2. Open-source compliance as first-class governance artifact
- Decision: Require dependency and license matrix in Phase 0, including redistribution status and replacement strategy.
- Rationale: Open-source publication is a core objective; licensing risk must be addressed before technical migration commits.
- Alternative considered: Handle licensing only near release.
- Why not: Late discoveries can force expensive rework or block publication.

3. Explicit out-of-scope registry
- Decision: Track intentionally excluded features in Phase 0 artifacts (Google GData explicitly excluded).
- Rationale: Prevents accidental migration scope creep and aligns stakeholder expectations.
- Alternative considered: Track exclusions informally in meeting notes.
- Why not: Non-auditable and easy to miss across teams.

4. UI direction alignment
- Decision: Phase planning and governance assumes legacy WebForms migration direction toward Vue 3 + ASP.NET Core APIs.
- Rationale: Keeps early governance outputs aligned with chosen front-end direction and dependency decisions.
- Alternative considered: Leave UI direction open through Phase 1.
- Why not: Defers critical dependency and architecture decisions needed for early spikes.

5. Benchmark before optimize
- Decision: Define the legacy-versus-modern benchmarking method in Phase 0 and require later phases to compare against the same baseline dataset and journey set.
- Rationale: Prevents moving performance evaluation to the end of the program where regressions are more expensive to resolve.
- Alternative considered: Benchmark only during hardening.
- Why not: Too late to influence architecture and slice-level implementation choices.

## Risks / Trade-offs

- [Gate overhead slows kickoff] -> Mitigation: Keep Phase 0 artifacts concise, with clear templates and owner deadlines.
- [Incomplete dependency inventory] -> Mitigation: Use package export + manual architecture review and require sign-off by platform/data/UI leads.
- [Ambiguous license interpretation] -> Mitigation: Add legal/compliance review checkpoint for uncertain licenses before Gate B approval.
- [Scope creep through undocumented exceptions] -> Mitigation: Maintain an explicit out-of-scope list with approval workflow for scope changes.

## Migration Plan

1. Create and approve migration charter with domain boundaries and out-of-scope registry.
2. Define RACI and assign Gate A/B/C owners.
3. Build dependency inventory and open-source compliance matrix.
4. Record initial replacement posture for proprietary components (replace/keep CE where license permits).
5. Define stakeholder review cadence and benchmarking checkpoints.
6. Validate Gate A entry criteria and launch Phase 1 work only after baseline approval.

Rollback strategy:
- If gates fail, stop phase progression and remain in planning mode.
- Rework failed artifacts (ownership, dependency decisions, compliance entries), re-run governance review, and re-evaluate gate status.

## Open Questions

- Which role provides final legal sign-off for ambiguous OSS/community license cases?
- What evidence format is required for "free community edition redistribution allowed" validation?
- Do we need a separate exception process for temporary proprietary tooling during transition?

## Context

Phase 1 follows governance and benchmarking setup. The migration task plan requires a documented baseline of applications, interfaces, jobs, dependencies, and operational knowledge before risk spikes or solution design work proceed. This phase provides the factual inventory and documentation set that later phases will reference.

## Goals / Non-Goals

**Goals:**
- Produce a current-state inventory of applications, interfaces, jobs, and external dependencies.
- Expand dependency ownership and license disposition beyond the Phase 0 governance baseline.
- Create a documentation baseline for architecture notes, runbooks, and migration decisions.
- Confirm out-of-scope exclusions before spikes begin.

**Non-Goals:**
- Execute technical spikes or implementation prototypes.
- Finalize architecture choices for UI, data, or auth.
- Build production-ready software components.

## Decisions

1. Baseline before spikes
- Decision: Require a current-state inventory and documentation baseline before Phase 2 spike work begins.
- Rationale: Reduces duplicated discovery effort and inconsistent assumptions across workstreams.
- Alternative considered: Let each spike discover its own baseline.
- Why not: Produces conflicting inventories and weakens phase gate evidence.

2. Documentation baseline is a deliverable, not a by-product
- Decision: Architecture notes, operational runbooks, and migration decisions are explicit outputs of this phase.
- Rationale: Later cutover and support work depends on reliable documentation, and delaying it increases risk.
- Alternative considered: Write docs only during implementation.
- Why not: Later phases would inherit undocumented assumptions and operational blind spots.

3. Scope exclusions remain explicit
- Decision: Reconfirm Google GData and any other excluded capabilities before Phase 2 planning.
- Rationale: Prevents accidental spike work in excluded areas.
- Alternative considered: Carry forward exclusions implicitly from Phase 0.
- Why not: Exclusions can drift unless reaffirmed in downstream plans.

4. Baseline acceptance is measured, not assumed
- Decision: Treat Phase 1 as complete only when inventory coverage, dependency ownership, and required documentation reach explicit acceptance thresholds.
- Rationale: Prevents vague sign-off and ensures later phases inherit an actually usable baseline.
- Alternative considered: Let leads approve based on general confidence.
- Why not: Leads to uneven downstream assumptions and weak gate evidence.

## Risks / Trade-offs

- [Inventory misses a legacy integration] -> Mitigation: Review baseline with platform, API, data, and UI leads before sign-off.
- [Documentation effort slows spike kickoff] -> Mitigation: Focus documentation on architecture, runbooks, and migration decisions that unblock later phases.
- [Dependency ownership remains unclear] -> Mitigation: Require each critical dependency row to have an owner before exit.

## Migration Plan

1. Build the application, interface, job, and external dependency inventory.
2. Expand the dependency matrix with replacement disposition and ownership.
3. Create the documentation baseline for architecture, operations, and migration decisions.
4. Reconfirm out-of-scope items and publish the phase exit package.
5. Approve Phase 1 exit before starting Phase 2 spikes.

Acceptance thresholds:
- Inventory coverage includes all user-facing applications, scheduled jobs, external integrations, and critical data flows needed by selected spike domains.
- Every critical third-party dependency row has an owner, license disposition, and migration strategy.
- Documentation baseline includes at minimum current-state architecture notes, operational runbooks for deployment, rollback, and support, and a migration decision log.
- Platform, API, data, and UI leads review and sign off the baseline package.

Rollback strategy:
- If the baseline is incomplete, stop Phase 2 entry and keep the program in readiness mode until inventory and documentation gaps are resolved.

## Open Questions

- Which legacy jobs and integrations are most critical to capture in the first baseline pass?
- Is there an existing document repository that should host the architecture/runbook baseline?

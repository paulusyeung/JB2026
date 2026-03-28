# Migration Charter - Phase 0

## Purpose
This charter establishes scope boundaries, governance gates, ownership expectations, and compliance constraints for the JB2015 to JB2026 migration.

## Objective
Migrate the legacy .NET Framework application to .NET 8 with phased coexistence while preparing JB2026 for open-source release.

## In Scope
- Governance and decision gates for migration phases.
- Baseline inventory and dependency/license readiness.
- Risk spikes for UI, data, auth/session, and API coexistence strategy.
- Backend, data, and UI migration in sequenced phases.
- Hardening, cutover readiness, and hypercare.

## Out of Scope
- Google GData feature migration.
- Broad UX research beyond representative migration slices.
- Broad cross-platform compatibility validation beyond required deployment/runtime targets.

## Constraints
- Runtime and build dependencies must be open-source compatible or approved free community editions.
- Coexistence and rollback must be available until cutover stability is confirmed.
- Security, benchmarking, and operational readiness gates are required before production cutover.

## Decision Gates
- Gate A: Architecture and migration-path viability.
- Gate B: Dependency and licensing readiness.
- Gate C: Cutover and operational readiness.

## Approvers
- Platform Lead
- API Lead
- Data Lead
- UI Lead
- QA/Performance Lead
- DevOps Lead
- Product Owner (business sign-off)

## Acceptance
Phase 0 is complete when required governance artifacts are approved and Gate A/B entry criteria are satisfied.

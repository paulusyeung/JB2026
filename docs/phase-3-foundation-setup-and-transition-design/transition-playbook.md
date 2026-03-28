# Transition Playbook

## Coexistence Boundaries
- Legacy system remains source of truth for domains not yet migrated.
- New .NET 8 services own only approved migration slices.
- Route ownership is explicit per endpoint family to avoid dual writes.

## Rollback Dependencies
- Versioned deployment artifacts for each service.
- Feature flags for migration slice activation.
- Backward-compatible API contracts during coexistence window.
- Database changes must include backward-compatible rollback scripts.

## Phase Handoffs
1. Phase 3 to Phase 4: solution scaffold, CI gates, environment model, observability baseline approved.
2. Phase 4 to Phase 5: backend feature slices migrated with parity validation.
3. Phase 5 to Phase 6: UI modernization slices accepted.
4. Phase 6 to Phase 7: hardening complete and operational readiness approved.

## Fallback Paths
- API fallback: route traffic back to legacy endpoints using gateway rules.
- Data fallback: revert write path to legacy data access layer for impacted slice.
- UI fallback: revert feature-flagged route to legacy experience.

## Entry/Exit Controls
- No phase transition without documented sign-off artifact.
- All high-priority risks must have mitigation or accepted-risk decisions.

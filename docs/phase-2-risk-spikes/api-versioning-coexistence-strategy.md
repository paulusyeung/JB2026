# API Versioning and Coexistence Strategy

## Scope
Defines route, coexistence, deprecation, and rollback rules while JB2015 and JB2026 APIs run in parallel.

## Versioning Rules
- New modern endpoints MUST use explicit major version route prefix: `/api/v1/...`.
- Legacy unversioned routes remain available during coexistence until deprecation criteria are met.
- Breaking changes require a new major prefix (`/api/v2/...`) and a migration notice.

## Coexistence Routing Rules
- Client routing contract:
  - Existing legacy clients continue to call legacy endpoints unless explicitly migrated.
  - New Vue 3 slices target versioned JB2026 routes by default.
- Gateway/reverse-proxy routing MUST support both route families simultaneously during migration windows.
- Domain owners are responsible for endpoint ownership maps covering legacy-modern overlap.

## Compatibility and Fallback
- Every migrated endpoint requires a documented legacy fallback target and owner.
- If modern endpoint health or parity degrades, traffic can be reverted to legacy endpoint for that slice.
- Coexistence period requires parity monitoring evidence before legacy retirement.

## Deprecation Rules
- Minimum deprecation package before removing a legacy endpoint:
  - migration notice and timeline
  - usage telemetry proving low/no active dependency
  - rollback validation evidence
  - owner approval and architecture board sign-off

## Rollback Rules
- Rollback trigger examples:
  - parity test regression in production path
  - unresolved auth/session incompatibility
  - sustained SLO breach for migrated route
- Rollback execution:
  - disable modern route flag
  - re-enable legacy route mapping
  - record incident and remediation in decision log

## Approval Status
- Approved for Phase 3 transition planning and pilot-slice reuse.
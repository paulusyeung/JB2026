## Why

Phase 1 has established the current-state baseline. The next critical step is to de-risk the highest-uncertainty technical domains before committing to broad migration execution: legacy WebForms to Vue 3 UI rewrite, DevExpress replacement, EF6 to EF Core data migration, auth/session model transition, and API versioning during coexistence. Without structured spikes and an API pilot, the core estimates and technology choices remain unvalidated assumptions.

## What Changes

- Introduce a time-boxed spike programme covering the highest-risk technical domains.
- Validate the legacy WebForms to Vue 3 migration path by building one representative screen end-to-end.
- Produce a DevExpress replacement evaluation and approved alternative selection.
- Validate EF Core 8 migration for a representative entity set including stored procedure interop.
- Define and approve the target auth/session architecture for ASP.NET Core.
- Deliver one API pilot slice with parity tests to prove the backend migration approach.
- Define the API versioning and coexistence strategy used while legacy and modern endpoints run side by side.

## Capabilities

### New Capabilities
- `vue3-ui-spike`: Validates the Vue 3 migration path for legacy WebForms screens through a working pilot screen.
- `devexpress-replacement-spike`: Evaluates and selects OSS/free community edition replacements for DevExpress reporting and charting.
- `efcore-migration-spike`: Validates EF Core 8 compatibility with the legacy EF6 model and stored procedures.
- `auth-session-spike`: Defines and approves target auth/session architecture for ASP.NET Core migration.
- `api-pilot-slice`: Delivers a production-quality pilot API endpoint migrated to ASP.NET Core with parity tests.
- `api-versioning-and-coexistence`: Defines versioning, routing, and deprecation rules while JB2015 and JB2026 endpoints coexist.

### Modified Capabilities
- None.

## Impact

- Unblocks broad migration execution across backend, data, and UI workstreams.
- Produces approved technology decisions for Vue 3, DevExpress replacement, EF Core, auth, and API coexistence.
- Produces first working ASP.NET Core API slice as a migration blueprint.

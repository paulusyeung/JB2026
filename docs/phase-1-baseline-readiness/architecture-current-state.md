# Current-State Architecture Notes

## Purpose
This note captures the current-state architecture baseline that downstream migration spikes will use as their common starting point.

## Source Landscape
- Legacy source system: `C:/Projects/JB2015` targeting .NET Framework 4.5.2.
- Modern target system: `C:/Projects/JB2026` targeting .NET 8 LTS.
- Delivery model: phased coexistence with feature-slice cutover rather than a big-bang replacement.

## Current Application Boundaries
| Legacy Component | Current Role | Modern Mapping | Notes |
|---|---|---|---|
| JB5.API | ASP.NET Web API 2 service surface | JB2026.Api | Primary API migration surface for parity testing and auth/session replacement |
| JB5.EF6 | EF6 EDMX-based data access/model layer | JB2026.EfCore | Primary data-layer spike target |
| JB5.REST | REST-oriented integration/service layer | JB2026.Rest | Likely coexistence and integration boundary during migration |
| Job.Book | Primary legacy web application with WebForms/MVC-era UI | JB2026.WebApp | Main UI modernization target, including DevExpress and CKEditor replacement |
| Job.Book.Mobile | Mobile-facing client/application surface | Target design to be confirmed in later phases | Depends on API and auth/session decisions |
| Job.Book.DAL | Shared data access and SQL interaction layer | JB2026.DataAccess | Supports EF Core migration and stored procedure interoperability |

## Cross-Cutting Technical Characteristics
- Legacy WebForms UI is the primary UI modernization risk and drives the Vue 3 spike.
- DevExpress v15.2 is a proprietary dependency that must not carry forward into the open-source target stack.
- EF6 EDMX models and legacy DAL patterns are the main data migration constraint.
- OWIN/Katana and Thinktecture IdentityModel anchor the current auth/CORS approach and must be replaced with native ASP.NET Core middleware.
- Background processing is assumed to include Hangfire-based scheduled jobs and needs confirmation against the legacy source during later discovery.

## Critical Runtime Paths
1. User-facing web flows originate in Job.Book and rely on legacy UI components plus API and data access services.
2. API workflows traverse JB5.API, JB5.EF6, and Job.Book.DAL before reaching the database.
3. External integration behavior is split across JB5.REST, JB5.API, and dependency-specific adapters.
4. Scheduled/background work is dependency-coupled through Hangfire and downstream data or integration paths.

## Migration Implications
- Phase 2 spikes must treat API, auth/session, UI, and data changes as coupled but independently testable slices.
- Dependency and license decisions need to be locked before broader foundation and UI work proceed.
- Current architecture knowledge is sufficient for bounded spike planning, but detailed endpoint, job, and external contract catalogs remain open documentation gaps.
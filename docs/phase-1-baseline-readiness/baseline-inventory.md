# Phase 1 Baseline Inventory

## Scope and Evidence
This inventory consolidates the current-state components, dependencies, scheduled work, and critical data flows referenced by the migration plan, the Phase 0 governance package, and the Phase 1 OpenSpec change.

Evidence sources used:
- `README.md`
- `task.md`
- Phase 0 governance artifacts under `docs/phase-0-governance/`

## Application Inventory
| Legacy Component | Type | Primary Responsibility | Modern Mapping | Phase 2 Spike Relevance | Owner Role |
|---|---|---|---|---|---|
| JB5.API | Service/API application | Web API 2 endpoints and service contracts | JB2026.Api | API pilot slice, auth/session spike, coexistence planning | API Lead |
| JB5.EF6 | Data access/model application | EF6 EDMX models and ORM mappings | JB2026.EfCore | EF Core migration spike | Data Lead |
| JB5.REST | Integration/API application | REST-facing integration surface | JB2026.Rest | API pilot slice and coexistence routing | API Lead |
| Job.Book | User-facing web application | Legacy UI, WebForms rewrite surface, DevExpress and CKEditor usage | JB2026.WebApp | Vue 3 UI spike, DevExpress replacement, CKEditor migration path | UI Lead |
| Job.Book.Mobile | Client/mobile surface | Mobile-facing workflows coupled to legacy API/auth behavior | Target mapping to be confirmed | Auth/session spike and API compatibility review | API Lead and UI Lead |
| Job.Book.DAL | Shared data library | SQL access patterns, stored procedures, shared persistence logic | JB2026.DataAccess | EF Core migration and API pilot data behavior | Data Lead |

## Scheduled Jobs and Operational Workloads
| Workload | Current Indicator | Modernization Relevance | Owner Role | Coverage Status |
|---|---|---|---|---|
| Hangfire background jobs | Identified in dependency baseline and README strategy | Must be reviewed for .NET 8 compatibility, scheduling semantics, and operational runbooks | Data Lead | Baseline coverage confirmed at platform level; exact job catalog remains a follow-up gap |

## External Dependencies and Integrations
| Integration or Dependency | Current Role | Phase 2 Relevance | Current Disposition |
|---|---|---|---|
| DevExpress v15.2 | Legacy reporting, charting, and UI controls | DevExpress replacement spike | Replace |
| CKEditor legacy integration | Rich-text editing in legacy UI | UI modernization planning | Evaluate replacement |
| Thinktecture IdentityModel | Legacy auth and CORS support | Auth/session spike | Replace |
| OWIN/Katana packages | Legacy middleware pipeline | Auth/session spike and API pilot | Replace |
| Google GData feature | Deprecated external feature | Explicitly excluded from spike scope | Out of scope |
| Hangfire | Scheduled/background execution | Runtime compatibility and operations | Keep or upgrade pending version verification |
| log4net | Logging stack | Foundation and observability planning | Keep or replace pending target logging decision |
| Magick.NET | Image processing dependency | API/runtime compatibility review | Keep or upgrade pending runtime validation |

## Critical Data Flows
| Flow | Current Path | Why It Matters |
|---|---|---|
| UI request flow | Job.Book -> JB5.API -> JB5.EF6 / Job.Book.DAL -> database | Establishes the parity path for UI, API, and data migration |
| Integration flow | JB5.REST / JB5.API -> external services | Drives coexistence routing and contract inventory needs |
| Background job flow | Hangfire -> data access / external integrations | Affects scheduler compatibility, rollback expectations, and support coverage |
| Mobile/API flow | Job.Book.Mobile -> legacy API and auth/session stack | Informs auth/session decisions and backward-compatibility scope |

## Coverage Assessment for Phase 2 Spike Domains
| Phase 2 Domain | Inventory Coverage | Coverage Decision |
|---|---|---|
| Vue 3 UI spike | Job.Book, DevExpress, CKEditor, UI request flow | Covered |
| DevExpress replacement spike | Job.Book, DevExpress dependency, UI flow | Covered |
| EF Core migration spike | JB5.EF6, Job.Book.DAL, data access flow | Covered |
| Auth/session spike | JB5.API, Job.Book.Mobile, Thinktecture IdentityModel, OWIN/Katana | Covered |
| API pilot slice | JB5.API, JB5.REST, Job.Book.DAL, integration flow | Covered |

## Inventory Conclusion
- The baseline inventory covers all currently identified applications, the known scheduled-job platform, external dependencies, and the critical data flows needed to plan the selected Phase 2 spike domains.
- Detailed endpoint, external contract, and per-job catalogs are not required to start bounded spike planning, but they remain documented follow-up gaps before later migration phases and final phase exit approval.
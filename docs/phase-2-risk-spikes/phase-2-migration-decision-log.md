# Phase 2 Migration Decision Log

| ID | Date | Decision | Rationale | Artifact |
|---|---|---|---|---|
| P2-D1 | 2026-03-27 | Vue 3 master-detail screen is validated as the representative UI spike path. | The spike demonstrates real API integration and key interaction parity for the selected legacy screen type. | `ui-spike-discovery-report.md` |
| P2-D2 | 2026-03-27 | DevExpress replacement baseline is AG Grid Community + Apache ECharts + PDFMake. | Best feature-coverage and OSS redistribution compatibility for data-heavy Job.Book workloads. | `devexpress-replacement-evaluation.md` |
| P2-D3 | 2026-03-27 | EF Core 8 DB-first strategy is viable for selected aggregate and stored procedure patterns. | Scaffolded model and automated CRUD/procedure tests passed against LocalDB schema. | `efcore-spike-validation.md` |
| P2-D4 | 2026-03-27 | Target auth/session approach is ASP.NET Core JWT bearer middleware for API access. | Removes OWIN/Katana dependencies and aligns with SPA client consumption model. | `auth-session-architecture-decision.md` |
| P2-D5 | 2026-03-27 | API migration slices must use explicit versioned routes (`/api/v1`) with coexistence and rollback rules. | Reduces contract ambiguity during legacy-modern parallel runtime. | `api-versioning-coexistence-strategy.md` |

## Recording Rule
Add one entry per architecture or migration decision with date, rationale, and linked artifact.
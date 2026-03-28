# Phase 2 DevExpress Replacement Evaluation

## Objective
Select an OSS or free community replacement strategy for DevExpress v15.2 before broad UI migration.

## Legacy Baseline Evidence
- `C:/Projects/JB2015/Job.Book/Web.config` contains DevExpress v15.2 runtime and control registrations.

## Candidate Evaluation Matrix
| Candidate | License | Data Grid Coverage | Chart Coverage | Report Generation Coverage | OSS Redistribution Compatibility | Outcome |
|---|---|---|---|---|---|---|
| AG Grid Community + Apache ECharts + PDFMake | MIT + Apache-2.0 + MIT | Strong for tabular and virtualized list screens | Strong for dashboard/stat charts | Moderate (custom template effort) | Yes | Recommended |
| PrimeVue + Chart.js + jsPDF | MIT + MIT + MIT | Strong for CRUD and form-heavy pages | Moderate to strong | Moderate (requires custom report templates) | Yes | Acceptable fallback |

## Recommendation
Adopt AG Grid Community + Apache ECharts + PDFMake as the primary replacement stack for Phase 6 planning.

## Rationale
- Strongest path for replacing legacy data-heavy list patterns found in Job.Book screens.
- No proprietary runtime dependency, with clear open-source licenses.
- Supports incremental rollout by component area rather than a single large-bang library swap.

## License Disposition
- DevExpress v15.2: proprietary, not redistribution-compatible for open-source target.
- Recommended stack: redistribution-compatible OSS licenses, acceptable for JB2026 open-source publication.

## Owner and Approval
- Owner: UI Lead
- License review disposition: Approved for Gate B planning package
## 1. Dependency and Build Setup

- [ ] 1.1 Add WebPivotTable dependency to ClientApp and update lockfile.
- [ ] 1.2 Verify local dev and production build resolve WebPivotTable entry/runtime assets.
- [ ] 1.3 Document any required bundler alias/import path for stable runtime behavior.

## 2. Invoice Stats OLAP Integration

- [ ] 2.1 Replace custom pivot table rendering in Invoice Stats view with WebPivotTable host integration.
- [ ] 2.2 Implement invoice-stats row transformation into WebPivotTable-compatible tabular payload.
- [ ] 2.3 Initialize WebPivotTable with legacy default layout (row/column/data fields and totals).
- [ ] 2.4 Preserve existing filter controls and refresh behavior using the current invoice-stats API source.

## 3. UX and Reliability

- [ ] 3.1 Implement loading/empty/error states around data load and OLAP initialization.
- [ ] 3.2 Keep or rewire export behavior so Invoice Stats export remains available after OLAP migration.
- [ ] 3.3 Add runtime guards/logging for invalid dataset shape to prevent silent failures.

## 4. Validation and Parity Checks

- [ ] 4.1 Validate default layout parity against legacy Invoice Stats expectations.
- [ ] 4.2 Validate totals parity on representative years/datasets including older-year rows.
- [ ] 4.3 Add or update tests for data mapping and successful OLAP initialization behavior.
- [ ] 4.4 Capture rollout notes and known limitations for maintainers.

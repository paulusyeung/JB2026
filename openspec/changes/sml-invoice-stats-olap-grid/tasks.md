## 1. Dependency and Build Setup

- [x] 1.1 Add WebPivotTable dependency to ClientApp and update lockfile.
- [x] 1.2 Verify local dev and production build resolve WebPivotTable entry/runtime assets.
- [x] 1.3 Document any required bundler alias/import path for stable runtime behavior.

## 2. Invoice Stats OLAP Integration

- [x] 2.1 Replace custom pivot table rendering in Invoice Stats view with WebPivotTable host integration.
- [x] 2.2 Implement invoice-stats row transformation into WebPivotTable-compatible tabular payload.
- [x] 2.3 Initialize WebPivotTable with legacy default layout (row/column/data fields and totals).
- [x] 2.4 Preserve existing filter controls and refresh behavior using the current invoice-stats API source.
- [x] 2.5 Use explicit WebPivotTable two-argument hydration signature (`attrArray`, `dataArray`) and await hydration before layout configuration.
- [x] 2.6 Add mount-time readiness synchronization (`customElements.whenDefined` + guarded retry) to prevent intermittent blank OLAP host.
- [x] 2.7 Remove implicit/default API row cap behavior and keep row limiting optional for large-dataset parity checks.

## 3. UX and Reliability

- [x] 3.1 Implement loading/empty/error states around data load and OLAP initialization.
- [x] 3.2 Keep or rewire export behavior so Invoice Stats export remains available after OLAP migration.
- [x] 3.3 Add runtime guards/logging for invalid dataset shape to prevent silent failures.
- [x] 3.4 Enforce explicit WebPivotTable host sizing (`display: block` + bounded height) so rendered grid is visible across viewport sizes.
- [x] 3.5 Set default OLAP display mode to grid for Invoice Stats parity.
- [x] 3.6 Format Amount aggregate output with thousand separators and 2 decimal places.

## 4. Validation and Parity Checks

- [ ] 4.1 Validate default layout parity against legacy Invoice Stats expectations.
- [ ] 4.2 Validate totals parity on representative years/datasets including older-year rows.
- [x] 4.3 Add or update tests for data mapping and successful OLAP initialization behavior.
- [x] 4.4 Capture rollout notes and known limitations for maintainers.

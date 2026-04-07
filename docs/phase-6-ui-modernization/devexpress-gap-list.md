# Phase 6 DevExpress Replacement Gap List

## Purpose

Confirm the validated Phase 2 findings and capture the concrete gaps between the earlier spike recommendation and the active Phase 6 implementation standard.

## Confirmed Phase 2 Inputs

- The Phase 2 UI spike proved the Vue 3 plus Vite path is viable for a representative master-detail workflow.
- The Phase 2 replacement evaluation correctly identified that DevExpress cannot remain in the open-source-ready runtime distribution.
- The Phase 2 evaluation favored AG Grid Community plus Apache ECharts plus PDFMake because those tools better cover high-density data and export scenarios.

## Phase 6 Implementation Baseline

- Active Phase 6 spec standardizes on Vuetify 3 for grids, forms, and dialogs.
- Chart.js is the selected charting library for dashboard slices.
- CKEditor 5 open-source build is the selected rich-text replacement.
- Feature flags remain mandatory to keep legacy routes reachable until each slice is approved.

## Gap List

1. Vuetify `v-data-table-server` covers basic sort, filter, and pagination, but it does not match AG Grid Community for column virtualization, column pinning, grouped data operations, or export workflows.
2. Large list screens that exceed roughly 500 rows still need a custom composable or slice-specific strategy for virtualization and persisted column state.
3. The Phase 2 evaluation assumed Apache ECharts for stronger chart breadth, but the Phase 6 spec now targets Chart.js. That reduces implementation surface area but leaves advanced reporting visuals to custom code.
4. The Phase 6 spec names FullCalendar resource and timeline views as the scheduler replacement target. Those capabilities are premium FullCalendar plugins rather than part of the Apache-2.0 core distribution, so the scheduler slice requires a product and licensing decision before it can be completed against the current spec text.
5. CKEditor 5 Classic covers the minimum toolbar set, but legacy CKEditor 4 plugin parity still needs slice-by-slice validation for embedded HTML, tables, and unsupported plugins.
6. The Vue spike did not include route guards, Playwright automation, or flag-aware routing. Those are now baseline deliverables for every migrated slice.

## SML Invoice Stats OLAP Notes

1. Invoice Stats now uses WebPivotTable via a custom element host (`web-pivot-table`) instead of the legacy handcrafted pivot table rendering.
2. Vite must resolve `webpivottable/dist/wpt.js` through the explicit alias `webpivottable-wpt` to avoid package `exports` resolution issues in local dev and production build.
3. Client import path is intentionally `import 'webpivottable-wpt'` with a corresponding `declare module 'webpivottable-wpt'` in `src/env.d.ts`.
4. The vendor bundle emits an `eval` warning from the upstream package and may increase chunk size for Invoice Stats; this is currently accepted for parity delivery.
5. Default OLAP layout is set at runtime for parity (rows: customer/invoice/PO/product/qty/unit/price; columns: year/month; value: amount sum).
6. If the primary grid init API path fails, the view falls back to `setWptFromDataArray` to reduce hard-failure risk.

## Conclusion

The Phase 2 findings remain directionally correct: Vue 3 is viable and DevExpress must be removed. The main Phase 6 execution risk is not the SPA scaffold; it is the remaining parity work for dense data grids, premium-style scheduler behavior, and rich-text edge cases.
## Context

SML Invoice Stats is a migrated ClientApp page that currently renders a handcrafted pivot-style table from invoice-stats API rows. Legacy behavior expects an OLAP-style grid interaction, including fixed default dimensions and totals visibility that users already rely on. The requested direction is to use WebPivotTable from bright-sea/webpivottable and keep the same backend data source so migration risk stays focused on presentation parity rather than data logic changes.

Constraints include preserving legacy-compatible default layout, handling potentially large datasets, and ensuring the package works in local Vite-based development and production builds.

## Goals / Non-Goals

**Goals:**
- Replace custom Invoice Stats pivot rendering with WebPivotTable in the ClientApp SML Invoice Stats page.
- Preserve existing filters and endpoint usage as the authoritative Invoice Stats data source.
- Apply a default OLAP configuration that mirrors the legacy invoice-stats dimensions and measure orientation.
- Ensure local installation and bundler-compatible loading path for the WebPivotTable asset.
- Keep CSV/export and totals parity behavior verifiable against legacy expectations.

**Non-Goals:**
- Rebuilding or changing backend Invoice Stats aggregation semantics.
- Replacing other SML pages with OLAP grid in this change.
- Introducing unrelated UI redesign of surrounding SML navigation/shell.

## Decisions

1. Use WebPivotTable as a custom-element integration inside the existing Vue view.
Rationale: This minimizes framework coupling and follows the component's intended usage model while allowing incremental adoption in one page.
Alternatives considered: fully custom Vue pivot rendering (already present but high maintenance); switching to a different OLAP library (larger evaluation scope and parity uncertainty).

2. Preserve existing API source and transform rows at the view boundary into grid-ready tabular input.
Rationale: Keeps backend stable, limits blast radius, and ensures legacy parity validation remains focused on presentation defaults.
Alternatives considered: adding new API shape for pivot config (not needed for current migration target).

3. Set deterministic default OLAP layout at initialization.
Rationale: Legacy parity requires predictable starting layout (rows: CustomerName/InvoiceNumber/PurchaseOrder/ProductCode/Qty/Unit/Price; columns: Year/Month; data: Amount with totals).
Alternatives considered: user-defined blank layout first (fails parity expectation).

4. Install package locally and include a compatibility loading path for bundling.
Rationale: WebPivotTable packaging and worker/assets can require explicit import/alias handling depending on build mode.
Alternatives considered: CDN-only loading (not acceptable for local/offline reproducibility).

## Risks / Trade-offs

- [Package bundling mismatch in Vite build] -> Mitigation: validate local dev/build imports and pin a known working package version with documented integration entrypoint.
- [Runtime init errors with invalid tabular input shape] -> Mitigation: centralize data transformation with explicit header + matrix output validation before component init.
- [Legacy subtotal/old-year parity drift due to row limits] -> Mitigation: preserve backend/default take semantics and include parity-focused checks for older-year totals.
- [Large dataset rendering performance] -> Mitigation: keep query filtering controls visible, use progressive loading state, and document operational take defaults.

## Migration Plan

1. Install WebPivotTable dependency in ClientApp and verify lockfile updates.
2. Integrate component into SML Invoice Stats view behind existing filter/query flow.
3. Implement row-to-OLAP mapping and apply default legacy layout configuration.
4. Validate local dev/build behavior and adjust import entrypoint/alias if required.
5. Run parity verification for key totals and export behavior before rollout.
6. Keep rollback path by preserving prior view logic in version control history; revert view integration commit if production issues appear.

## Open Questions

- Which exact package version of WebPivotTable is most stable with the current Vite toolchain in this repository?
- Should user-customized OLAP layouts be persisted (for example local storage) now or deferred to a follow-up?
- Is parity acceptance based only on totals/layout defaults, or must every legacy export column formatting quirk be replicated in this phase?

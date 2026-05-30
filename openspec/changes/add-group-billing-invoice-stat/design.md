## Context

The ClientApp already has two nearby pieces that make this change low risk: the Billing area already loads invoice summaries through `listInvoices()`, which is served by the JB2026 billing API and backed by Invoice Ninja, and the SML Invoice Stats page already embeds WebPivotTable with resilient initialization, fixed layout configuration, and app-aligned loading states. The requested Billing `Invoice Stats` screen is narrower than the SML version because it has no filters and only needs invoice-level fields plus `Year` and `Month` derived from `InvoiceDate`.

The main constraint is to keep the implementation minimal and preserve current behavior. That means the existing SML Invoice Stats screen should not be repurposed in a way that changes its data contract or layout, and the billing change should prefer reusing proven presentation logic over introducing a separate analytics stack.

## Goals / Non-Goals

**Goals:**
- Add a new Group Billing navigation target for `Invoice Stats`.
- Present billing invoice summaries in an OLAP grid consistent with the existing Invoice Stats styling and structure.
- Use the existing billing invoice list source, scoped initially to current-year invoices in `Sent` status, and derive `Year` and `Month` from `InvoiceDate` in the frontend mapping layer.
- Start with a fixed layout and no filter controls so the feature matches the requested scope exactly.
- Show a stable `Unknown` period label when billing rows are missing a usable invoice date.

**Non-Goals:**
- Changing the existing Job Order > SML > Invoice Stats page.
- Adding new billing backend aggregation endpoints if the current invoice summary list remains sufficient.
- Introducing ad hoc filtering, editable pivot presets, or broader billing analytics beyond the requested layout.

## Decisions

1. Reuse the existing WebPivotTable integration pattern from SML Invoice Stats for the new billing page.
Rationale: The current SML implementation already handles custom-element loading, hydration retries, and theme-aware pivot configuration. Reusing that pattern minimizes UI and runtime risk.
Alternatives considered: building a separate static table or a second analytics implementation, which would duplicate behavior and create more maintenance surface.

2. Use the existing billing invoice summary list as the source dataset, with an initial frontend-side scope to current-year invoices in `Sent` status.
Rationale: `InvoiceBillingSummary` already provides `clientName`, `invoiceNumber`, `invoiceDate`, `amount`, and `status`, and that list is already backed by Invoice Ninja through the JB2026 billing API. This keeps Invoice Ninja as the system of record while avoiding direct browser calls to the external service.
Alternatives considered: loading the entire invoice list into the pivot, which may degrade browser responsiveness as billing volume grows; adding a dedicated billing stats endpoint, which would expand scope without clear need for the requested first version.

3. Keep the billing Invoice Stats layout fixed and filterless for the initial implementation.
Rationale: The user explicitly requested an empty filter section and a specific pivot layout. A fixed configuration keeps the feature narrowly scoped and reduces parity ambiguity.
Alternatives considered: carrying over SML lookup/date filters or exposing additional pivot controls, which would be extra behavior not requested here.

4. Derive `Year` and `Month` at the view-mapping boundary from `InvoiceDate`, using a consistent `Unknown` label for missing or invalid dates.
Rationale: The billing list source should remain unchanged, while the stats page can normalize missing or invalid dates into predictable pivot values without showing empty pivot buckets.
Alternatives considered: pushing derived fields into the billing API response, which is unnecessary for a minimal frontend-first change.

## Risks / Trade-offs

- [Billing invoice summaries omit or inconsistently format `invoiceDate`] -> Mitigation: normalize date parsing in one mapping function and surface a consistent `Unknown` period label without failing the page.
- [Copy-pasting too much from SML Invoice Stats creates divergence later] -> Mitigation: extract only the smallest shared pivot helper/view structure needed, or keep the new page narrowly cloned if that produces less churn.
- [Large billing invoice lists render slowly in the pivot] -> Mitigation: start with current-year `Sent` invoices only, and verify the typical `listInvoices()` payload size before deciding whether backend narrowing is needed.
- [Billing users expect list-style actions on the stats page] -> Mitigation: keep the page clearly positioned as a read-only summary view and leave invoice actions in existing billing list/detail screens.

## Migration Plan

1. Add the new billing route, menu item, and localized title key for `Invoice Stats`.
2. Implement the billing invoice-stats view by reusing the existing OLAP page structure with billing-specific data mapping, current-year `Sent` invoice scoping, and fixed layout defaults.
3. Validate that opening Group Billing > Invoice Stats loads the expected billing invoice summaries into the pivot without affecting the SML Invoice Stats route.
4. If issues appear, roll back by removing the new route/menu entry and billing view while leaving all existing billing and SML screens unchanged.

## Open Questions

- Does the current `listInvoices()` payload size stay comfortably within browser-friendly limits after restricting the page to current-year `Sent` invoices?
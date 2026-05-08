# Page Banner Classification Inventory

This document provides the concrete A/B/C classification for all authenticated route-backed views in ClientApp.

**Definitions:**
- **Class A**: Safe full banner removal — the view's top `v-card-title` contains only the page title/subtitle intro block, no controls.
- **Class B**: Remove title/subtitle only — the view's top `v-card-title` contains both the banner and functional controls (filters, buttons, etc.); remove only the banner `div`, preserve the controls layout.
- **Class C**: No change — the view's `h3` / subtitle blocks are section headings inside content cards, functional toolbars, or special patterns; do not modify.

---

## Class A: Safe Full Banner Removal

Remove the entire top `v-card-title` block (or just the internal `div` containing `h3` + subtitle) and tighten spacing if needed.

1. **JobListView.vue** — `jobOrder.jobList.title` / `jobOrder.jobList.subtitle`
2. **OrderListView.vue** — `jobOrder.orderList.title` / `jobOrder.orderList.subtitle`
3. **SchedulePendingView.vue** — `jobOrder.pending.title` / `jobOrder.pending.subtitle`
4. **ScheduleCompletedView.vue** — `jobOrder.completed.title` / `jobOrder.completed.subtitle`
5. **SchedulePackingView.vue** — `jobOrder.packing.title` / `jobOrder.packing.subtitle`
6. **JobStatsView.vue** — `jobOrder.jobStats.title` / `jobOrder.jobStats.subtitle`
7. **StockView.vue** — `stock.title` / `stock.subtitle`
8. **QuotationsView.vue** — `quotations.title` / `quotations.subtitle`
9. **AdminWorkflowView.vue** — `admin.workflow.title` / `admin.workflow.subtitle`
10. **AdminWorkflowFormsView.vue** — `admin.workflowForms.title` / `admin.workflowForms.subtitle`
11. **AdminUserView.vue** — `admin.user.title` / `admin.user.subtitle`
12. **AdminSupplierView.vue** — `admin.supplier.title` / `admin.supplier.subtitle`
13. **AdminQuotationItemView.vue** — `admin.quotationItem.title` / `admin.quotationItem.subtitle`
14. **AdminQuotationItemGroupView.vue** — `admin.quotationItemGroup.title` / `admin.quotationItemGroup.subtitle`
15. **SettingsView.vue** — `settings.title` / `settings.subtitle`
16. **HelpView.vue** — `help.title` / `help.subtitle`
17. **SmlInvoiceStatsView.vue** — `sml.invoiceStats.title` / `sml.invoiceStats.subtitle`
18. **SmlRtfStatsView.vue** — `sml.rtfStats.title` / `sml.rtfStats.subtitle`

---

## Class B: Remove Banner Sub-Block Only (Preserve Controls)

Remove only the `div` containing the `h3` + subtitle; keep the rest of the `v-card-title` structure intact (controls, spacers, buttons remain). You may need to adjust flex/alignment rules if the controls row relied on the banner for left anchoring.

1. **ReportsView.vue** — `reports.title` / `reports.subtitle`
   - Controls in same row: date field + "Run" button
   - Keep: controls and flex layout

2. **ExceptionalReportView.vue** — `reports.exceptional.title` / `reports.exceptional.subtitle`
   - Controls in same row: month picker + "Refresh" button
   - Keep: controls and flex layout

3. **PublicView.vue** — `publicContent.title` / `publicContent.subtitle`
   - Controls in same row: "Refresh" button
   - Keep: controls and flex layout

4. **JobOrderView.vue** — `jobOrder.title` / `jobOrder.subtitle`
   - Controls in same row: search field + "Refresh" button
   - Keep: controls and flex layout

5. **SmlView.vue** — `sml.title` / `sml.subtitle`
   - Controls in same row: date input + "Refresh" button
   - Keep: controls and flex layout

6. **SmlInvoiceListView.vue** — `sml.invoiceList.title` / `sml.invoiceList.subtitle`
   - Controls in same row: "Refresh" button
   - Keep: controls and flex layout

---

## Class C: Do Not Modify

These views contain section headings, functional toolbars, or special patterns that are NOT duplicate page banners. Leave them unchanged.

1. **DashboardView.vue**
   - The `h3 + subtitle` you see is a **chart card title** ("Volume Trend"), not a page-level intro banner.
   - Reason: it's inside a secondary card, not the page's top banner.
   - Action: **Keep as-is.**

2. **JobsView.vue**
   - The `h3 + subtitle` belongs to a **detail panel** on the side of the main JobsTable, not a page-level header.
   - Reason: it's inside a layout grid cell, not a top-page banner.
   - Action: **Keep as-is.**

3. **ScheduleView.vue**
   - The `v-card-title` is a **functional toolbar** with Save button, machine toggles, and Refresh.
   - It has an eyebrow (scheduler.schedule.*) but no subtitle; this is a tool header, not a redundant banner.
   - Reason: controls are primary, not secondary; title serves as context for the toolbar.
   - Action: **Keep as-is** (or handle explicitly in a future design pass if needed).

4. **SchedulePackingOnAirView.vue**
   - The `v-card-title` is a **functional toolbar** (Save + Refresh buttons).
   - Reason: controls are primary, not secondary.
   - Action: **Keep as-is.**

5. **SchedulerView.vue**
   - Uses a page-hero style `h3.text-h5` with eyebrow + subtitle; this is a special pattern, not the repeated `h3.text-h6` banner structure.
   - Reason: distinct visual style and purpose.
   - Action: **Keep as-is** (handle separately if title consolidation is needed later).

6. **LegacySliceView.vue**
   - Contains multiple section-specific headers and content blocks.
   - Reason: each section has its own identity; this is not a single page banner.
   - Action: **Keep as-is.**

7. **LegacyMenuPlaceholderView.vue**
   - Already computes `pageTitle` from route metadata inside the view; this is a legacy placeholder UX.
   - Reason: special handling as a transitional placeholder.
   - Action: **Keep page banner for now** or deprecate after confirming route is not in use.

---

## Implementation Checklist

Use this checklist during implementation:

- [ ] **Topbar updates** (section 1 of tasks): Update eyebrow and title binding in `AppTopbar.vue`
- [ ] **Class A removal** (18 files): Remove entire top banner block for each file
- [ ] **Class B surgery** (6 files): Remove only banner `div`, preserve controls
- [ ] **Class C verification** (7 files): Confirm no changes were made to these files
- [ ] **Spacing review**: Check all modified views for vertical rhythm after removal
- [ ] **Locale switching**: Confirm topbar title updates on language change
- [ ] **Route navigation**: Confirm topbar title updates on route navigation
- [ ] **Fallback test**: Navigate to/create a route without a title key; confirm topbar shows `common.appName`

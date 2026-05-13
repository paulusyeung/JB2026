## Why

The initial theme-aware color standardization change successfully refactored high-impact views (QuotationsView, admin workflow components, and global styles). However, 16+ additional views still contain hardcoded RGBA color values that don't respond to theme changes. This follow-up ensures theme consistency across the entire application.

## What Changes

- Refactor all remaining list views with hardcoded header RGBA colors (light: `rgba(195,216,248,0.92)`, dark: `rgba(52,74,104,0.95)`) to use Vuetify theme variables.
- Update SettingsView RGBA gradient backgrounds to theme-aware equivalents.
- Update DashboardView chart grid colors from hardcoded RGBA to Vuetify theme tokens.
- Apply the same fallback tuple cleanup pattern to any remaining admin workflow or form components.

## Capabilities

### Modified Capabilities
- `theme-aware-color-standardization`: Extend theme-aware color compliance to all remaining UI surfaces.

## Impact

- Affected frontend code (in-scope):
  - **List views (16 files)**: `JobListView.vue`, `OrderListView.vue`, `SmlInvoiceListView.vue`, `SmlRtfListView.vue`, `SchedulePackingView.vue`, `ScheduleCompletedView.vue`, `SchedulePendingView.vue`, `StockView.vue`, `AdminWorkflowView.vue`, `AdminWorkflowFormsView.vue`, `AdminCustomerView.vue`, `AdminSupplierView.vue`, `AdminUserView.vue`, `AdminQuotationItemGroupView.vue`, `AdminQuotationItemView.vue`, `ExceptionalReportView.vue` — all share identical hardcoded RGBA header pattern.
  - `SettingsView.vue` — hardcoded RGBA gradient backgrounds.
  - `DashboardView.vue` — hardcoded RGBA chart grid colors.
- No backend/API contract changes.
- Expected outcome is complete theme consistency across all views.

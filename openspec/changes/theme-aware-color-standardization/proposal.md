## Why

The UI currently shows inconsistent colors when switching between Light and Dark themes because multiple component and global styles use hardcoded color values. Standardizing color usage on Vuetify theme tokens will remove visual mismatch, reduce maintenance overhead, and make theme behavior predictable.

## What Changes

- Refactor component styles that use hardcoded color values to use Vuetify theme CSS variables.
- Replace hardcoded layout and shell background or border colors in shared styles with theme-aware equivalents.
- Remove stale RGB fallback values that prevent colors from updating correctly after theme changes.
- Add regression validation for key views in both Light and Dark themes.

## Capabilities

### New Capabilities
- `theme-aware-color-standardization`: Ensure visible UI surfaces, text, and emphasis colors consistently react to theme changes by using Vuetify theme variables.

### Modified Capabilities
- None.

## Impact

- Affected frontend code (in-scope):
  - `JB2026.WebApp/ClientApp/src/views/QuotationsView.vue` — hardcoded RGBA header gradients
  - `JB2026.WebApp/ClientApp/src/styles/main.scss` — custom `--shell-*` variables (dual-source, not Vuetify)
  - `JB2026.WebApp/Views/Shared/_Layout.cshtml.css` — fully hardcoded Bootstrap hex colors, no theme awareness
  - `ClientApp/src/components/forms/AdminWorkflowRecordDialog.vue` — stale RGB fallback tuples (e.g. `rgb(var(--v-theme-surface, 245, 245, 245))`)
  - `ClientApp/src/components/forms/AdminWorkflowFormsDialog.vue` — stale RGB fallback tuples
  - `ClientApp/src/components/forms/AdminWorkflowFormDesignerDialog.vue` — hardcoded `rgba(128, 128, 128, ...)` and `rgba(100, 100, 200, ...)`
- Additional views with identical hardcoded RGBA header pattern (light `rgba(195,216,248,0.92)`, dark `rgba(52,74,104,0.95)`), excluded from initial scope but eligible for follow-up:
  - `JobListView.vue`, `OrderListView.vue`, `SmlInvoiceListView.vue`, `SmlRtfListView.vue`, `SchedulePackingView.vue`, `ScheduleCompletedView.vue`, `SchedulePendingView.vue`, `StockView.vue`, `AdminWorkflowView.vue`, `AdminWorkflowFormsView.vue`, `AdminCustomerView.vue`, `AdminSupplierView.vue`, `AdminUserView.vue`, `AdminQuotationItemGroupView.vue`, `AdminQuotationItemView.vue`, `ExceptionalReportView.vue`
  - `SettingsView.vue` — hardcoded RGBA gradient backgrounds
  - `DashboardView.vue` — hardcoded RGBA chart grid colors
- No backend/API contract changes.
- No backend/API contract changes.
- Expected outcome is improved visual consistency, reduced custom theme override logic, and simpler future styling work.

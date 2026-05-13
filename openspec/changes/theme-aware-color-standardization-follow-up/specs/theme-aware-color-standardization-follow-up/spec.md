## ADDED Requirements

### Requirement: Theme-aware color rendering for remaining list views
The system SHALL render visible UI colors for all remaining list views (`JobListView`, `OrderListView`, `SmlInvoiceListView`, `SmlRtfListView`, `SchedulePackingView`, `ScheduleCompletedView`, `SchedulePendingView`, `StockView`, `AdminWorkflowView`, `AdminWorkflowFormsView`, `AdminCustomerView`, `AdminSupplierView`, `AdminUserView`, `AdminQuotationItemGroupView`, `AdminQuotationItemView`, `ExceptionalReportView`) using Vuetify theme CSS variables so Light and Dark mode switches apply consistently.

#### Scenario: List view header color adaptation on theme switch
- **WHEN** a user switches between Light and Dark themes on a list view
- **THEN** table headers and view surfaces MUST update to the active theme without stale hardcoded colors
- **AND** all text-to-background color pairs MUST meet WCAG AA minimum contrast ratio of 4.5:1 for normal text and 3:1 for large text

### Requirement: Theme-aware gradient backgrounds
The system SHALL apply theme-aware background colors for SettingsView RGBA gradient backgrounds and DashboardView chart grid overlays.

#### Scenario: Settings and dashboard surfaces in dark theme
- **WHEN** the application renders SettingsView and DashboardView in Dark theme
- **THEN** background gradients and chart grids MUST resolve from active theme variables instead of static RGBA values

### Requirement: Regression validation for follow-up views
The system SHALL verify all follow-up UI routes in both Light and Dark themes to confirm readability and visual consistency after color standardization changes.

#### Scenario: Dual-theme regression pass for follow-up views
- **WHEN** regression validation is executed for each follow-up route
- **THEN** no route MUST exhibit unreadable text or mismatched surfaces caused by hardcoded style values
- **AND** visual appearance MUST be consistent with the initial phase refactored views

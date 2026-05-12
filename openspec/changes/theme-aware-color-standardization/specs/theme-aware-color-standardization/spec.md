## ADDED Requirements

### Requirement: Theme-aware color rendering
The system SHALL render visible UI colors for all in-scope views (`QuotationsView`, admin workflow components, shared shell/layout surfaces, server-rendered layout) using Vuetify theme CSS variables so Light and Dark mode switches apply consistently.

#### Scenario: View-level color adaptation on theme switch
- **WHEN** a user switches between Light and Dark themes on an in-scope view
- **THEN** table headers, view surfaces, and view text colors MUST update to the active theme without stale hardcoded colors
- **AND** all text-to-background color pairs MUST meet WCAG AA minimum contrast ratio of 4.5:1 for normal text and 3:1 for large text

### Requirement: Shared layout surfaces follow active theme
The system SHALL apply theme-aware background and border colors for shared shell and layout styles used by the web application.

#### Scenario: Global shell colors in dark theme
- **WHEN** the application renders shared shell and layout components in Dark theme
- **THEN** backgrounds and borders MUST resolve from active theme variables instead of static hex values

### Requirement: Removal of stale color fallbacks
The system SHALL avoid hardcoded RGB tuple fallbacks in theme-dependent color expressions where fallback values can prevent proper runtime theme adaptation.

#### Scenario: Theme token fallback cleanup
- **WHEN** theme-dependent styles are evaluated after switching theme
- **THEN** color expressions MUST use active theme token values without preserving outdated fallback tuples

### Requirement: Regression validation for theme parity
The system SHALL verify all in-scope UI routes in both Light and Dark themes to confirm readability and visual consistency after color standardization changes. For each validated route, a documented screenshot or visual baseline MUST be recorded in both themes.

#### Scenario: Dual-theme regression pass
- **WHEN** regression validation is executed for each in-scope route
- **THEN** no route MUST exhibit unreadable text or mismatched surface colors caused by hardcoded style values
- **AND** a before/after screenshot pair MUST be recorded per route to establish visual baseline

## ADDED Requirements

### Requirement: Schedule lists use shared mobile card pattern on phone
On phone layouts, Available and Scheduled job lists in the schedule board SHALL use the shared `ListMobileCard` pattern (or a thin wrapper around it) rather than raw HTML table rows inside `<tbody>`.

#### Scenario: Available jobs render as cards in bottom sheet
- **WHEN** the Available jobs bottom sheet is open on phone layout
- **THEN** each available job is rendered as a mobile card with labeled fields consistent with other list views

#### Scenario: Scheduled jobs render as cards in main flow
- **WHEN** the schedule board is on phone layout
- **THEN** each scheduled job in the main Scheduled section is rendered as a mobile card

#### Scenario: Desktop scheduled table unchanged
- **WHEN** the schedule board is above the `sm` breakpoint
- **THEN** Scheduled jobs continue to use the resizable HTML table with existing column behavior

### Requirement: Mobile schedule cards support selection where applicable
Mobile card presentation for Available jobs SHALL support multi-select via checkbox consistent with the desktop available table.

#### Scenario: Select available job on card
- **WHEN** the user toggles the checkbox on an Available job card
- **THEN** that job is included in the selected set used for transfer actions

#### Scenario: Select-all in sheet
- **WHEN** the user invokes select-all in the Available bottom sheet
- **THEN** all visible Available jobs become selected

### Requirement: Schedule mobile cards expose configurable field columns
Schedule mobile cards SHALL declare fields via column configuration (keys, labels, formatters) aligned with `ListMobileCardColumn` conventions used in Tier 1 list views.

#### Scenario: Field labels are localized
- **WHEN** mobile cards render order, customer, title, and machine fields
- **THEN** labels use existing scheduler i18n keys where available

## ADDED Requirements

### Requirement: Scheduled-first layout on phone viewports
On phone layouts (`smAndDown`), the schedule board SHALL present the Scheduled jobs list as the primary in-page content and SHALL NOT render the Available jobs panel in the main document flow.

#### Scenario: Phone shows scheduled list only in main flow
- **WHEN** the user opens the schedule board on a viewport at or below the `sm` breakpoint
- **THEN** the Scheduled panel is visible in the main page flow
- **AND** the Available panel is not rendered as a stacked section above or below Scheduled in the main flow

#### Scenario: Desktop retains three-panel board
- **WHEN** the user opens the schedule board above the `sm` breakpoint
- **THEN** Available, transfer controls, and Scheduled panels remain in the existing desktop layout

### Requirement: Available jobs are selected via a bottom sheet on phone
On phone layouts, the system SHALL provide a primary control (e.g., "Add jobs") that opens a `v-bottom-sheet` containing the Available jobs list with selection support.

#### Scenario: Open available jobs sheet
- **WHEN** the user taps the Add jobs control on phone layout
- **THEN** a bottom sheet opens showing Available jobs with checkboxes or equivalent selection
- **AND** the sheet content respects safe-area inset padding at the bottom

#### Scenario: Close sheet after successful transfer
- **WHEN** the user completes a move-to-scheduled action from the sheet with at least one selected job
- **THEN** the bottom sheet closes
- **AND** the moved job(s) appear in the Scheduled list without requiring the user to scroll to a separate panel

### Requirement: Machine transfer uses a touch-friendly action menu on phone
On phone layouts, the system SHALL NOT rely on the vertical M1–M5 icon button column for transfers. Instead, it SHALL expose machine targets and related move actions through a touch-friendly menu (`JobActionMenu`) with minimum 44px tap targets.

#### Scenario: Transfer selected jobs to a machine from phone
- **WHEN** the user has one or more Available jobs selected in the bottom sheet
- **AND** the user chooses a machine target from the action menu
- **THEN** the selected jobs move to Scheduled with the chosen machine assignment in local state

#### Scenario: Desktop transfer column unchanged
- **WHEN** the user operates the schedule board above the `sm` breakpoint
- **THEN** the existing transfer button column behavior remains available

### Requirement: Critical scheduled fields are visible on phone
On phone layouts, Scheduled job presentation SHALL include print quantity, color, and size (Qty, Color, Size) without requiring horizontal scrolling of a wide table.

#### Scenario: Scheduled card shows print fields
- **WHEN** a scheduled job is shown in phone card/list mode
- **THEN** print quantity, color, and size are visible in the card body or header

#### Scenario: No hidden-by-breakpoint print columns on phone
- **WHEN** the schedule board is in phone layout
- **THEN** print Qty, Color, and Size are not suppressed solely by `isPhoneLayout` conditionals

### Requirement: Local transfer feedback before explicit Save
The schedule board SHALL update Available and Scheduled local collections immediately when the user performs a transfer action, before the user clicks Save. Persisting to the server SHALL still occur only via the existing Save action calling `saveScheduleBatch`.

#### Scenario: Immediate UI update on transfer
- **WHEN** the user moves jobs between Available and Scheduled (any layout)
- **THEN** both lists reflect the change immediately in the UI

#### Scenario: Save failure preserves server truth
- **WHEN** the user clicks Save and `saveScheduleBatch` fails
- **THEN** the UI shows a user-visible error
- **AND** the UI restores consistency with server state via rollback or reload without silent data loss

### Requirement: No desktop-preferred notice on schedule board when adaptive workflow is active
When the adaptive phone workflow is enabled, `ScheduleView` SHALL NOT display the desktop-preferred info alert on narrow phones.

#### Scenario: Phone schedule board has no desktop-preferred banner
- **WHEN** the user views the schedule board at or below the `sm` breakpoint
- **THEN** no desktop-preferred scheduling notice is shown

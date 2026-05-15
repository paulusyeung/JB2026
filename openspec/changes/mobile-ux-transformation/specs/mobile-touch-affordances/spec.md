## ADDED Requirements

### Requirement: Touch composable does not control layout mode
The `useTouch` composable SHALL expose touch capability and safe-area measurements only. Layout switching between phone and desktop presentations SHALL be determined by Vuetify `useDisplay` (or equivalent breakpoint logic), not by touch detection alone.

#### Scenario: Touch laptop uses desktop layout
- **WHEN** the device reports touch capability and viewport width is above the `sm` breakpoint
- **THEN** the schedule board uses the desktop layout regardless of `isTouchDevice`

#### Scenario: Non-touch narrow viewport uses phone layout
- **WHEN** viewport width is at or below the `sm` breakpoint and the device does not report touch
- **THEN** the phone layout still applies

### Requirement: Safe-area inset is available for bottom sheets
The touch composable or shared CSS utilities SHALL provide bottom safe-area padding suitable for bottom sheets and fixed footers on notched devices.

#### Scenario: Bottom sheet clears home indicator
- **WHEN** a bottom sheet is open on a device with a non-zero safe-area inset bottom
- **THEN** sheet actions and content are not obscured by the system home indicator

### Requirement: Primary scheduler actions meet minimum touch target size on phone
On phone layouts, primary scheduler actions (Add jobs, Save, machine selection in `JobActionMenu`) SHALL have an effective tap target of at least 44×44 CSS pixels.

#### Scenario: Action menu items are tappable
- **WHEN** the user opens `JobActionMenu` on phone layout
- **THEN** each machine/action list item has at least 44px height or padding equivalent

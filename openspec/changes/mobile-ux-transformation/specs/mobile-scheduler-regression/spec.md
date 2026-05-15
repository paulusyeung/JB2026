## ADDED Requirements

### Requirement: Automated mobile test covers scheduler transfer flow
The ClientApp Playwright suite SHALL include a mobile viewport test that exercises the schedule board adaptive workflow: open Available sheet, select a job, transfer to a machine, and verify appearance in Scheduled.

#### Scenario: Mobile scheduler smoke path
- **WHEN** the test runs under the mobile viewport project against the schedule board route
- **THEN** it opens the Add jobs bottom sheet (or equivalent control)
- **AND** selects at least one available job
- **AND** performs a transfer via the mobile action menu
- **AND** asserts the job is listed in the Scheduled section

### Requirement: Mobile tests cover absence of schedule desktop-preferred notice
The mobile viewport test suite SHALL assert that the schedule board does not render the desktop-preferred scheduling notice at phone widths after this change ships.

#### Scenario: No desktop-preferred alert on schedule route
- **WHEN** the mobile test loads the schedule board at phone width
- **THEN** no element matching the desktop-preferred scheduling notice text is visible

### Requirement: Mobile tests validate print field visibility on scheduled cards
The mobile viewport test suite SHALL assert that scheduled mobile presentation includes print quantity, color, or size labels/values for a fixture row when data is present.

#### Scenario: Scheduled card shows print metadata
- **WHEN** the mobile test displays a scheduled job with print fields populated
- **THEN** the scheduled card or row surface includes visible Qty, Color, and Size information

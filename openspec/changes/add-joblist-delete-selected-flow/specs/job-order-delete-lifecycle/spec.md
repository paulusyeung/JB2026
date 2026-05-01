## ADDED Requirements

### Requirement: Job List delete action SHALL remain visible and be disabled without selection
The system SHALL render a Delete action in Job List controls as a persistent action and SHALL disable it whenever no selected job order exists.

#### Scenario: No selection keeps delete disabled
- **WHEN** the Job List loads and no rows are selected
- **THEN** the Delete action SHALL be visible and disabled

#### Scenario: Selection enables delete
- **WHEN** one or more job orders are selected
- **THEN** the Delete action SHALL be enabled unless a delete operation is currently running

#### Scenario: Delete remains visible outside checkbox mode
- **WHEN** the user is not in checkbox mode
- **THEN** the Delete action SHALL remain visible and SHALL be disabled unless one or more job orders are selected

### Requirement: Delete execution SHALL require explicit user confirmation
The system SHALL request confirmation before deleting selected job orders.

#### Scenario: User cancels delete confirmation
- **WHEN** the user invokes Delete and rejects the confirmation prompt
- **THEN** no selected job order SHALL be deleted

#### Scenario: User confirms delete
- **WHEN** the user invokes Delete and accepts the confirmation prompt
- **THEN** the system SHALL execute delete for the selected job order ids

### Requirement: Job order delete lifecycle SHALL remove related workflow and attachment resources
The system SHALL perform parity-aligned server-side cleanup for each deleted job order, including dependent workflow rows, attachment rows, and attachment image files from storage.

#### Scenario: Deleting a job order with workflow rows
- **WHEN** a selected job order has related workflow records
- **THEN** all related workflow records SHALL be removed before delete completes

#### Scenario: Deleting a job order with attachment files
- **WHEN** a selected job order has one or more attachments with stored files/images
- **THEN** attachment metadata rows and corresponding files/images in storage SHALL be removed before delete completes

### Requirement: Job order delete SHALL remove the record and maintain sibling numbering parity
The system SHALL delete the job order record and SHALL rebuild sibling job numbers as required by legacy parity behavior.

#### Scenario: Deleting a sibling in a multi-job order
- **WHEN** a deleted job order has `jobNumber > 0` and there are sibling jobs with higher numbers
- **THEN** sibling jobs with higher numbers SHALL be renumbered to close gaps

#### Scenario: Deleting a non-sibling job does not trigger renumber
- **WHEN** a deleted job order has `jobNumber = 0` or `jobNumber is null`
- **THEN** no sibling renumber operation SHALL occur

### Requirement: Batch delete SHALL process selected items and report aggregate result
The system SHALL support deleting multiple selected job orders and SHALL continue processing remaining items when one item fails.

#### Scenario: Batch delete with mixed outcomes
- **WHEN** multiple selected job orders are processed and at least one item fails
- **THEN** successful items SHALL remain deleted, failed items SHALL remain, and UI feedback SHALL include aggregate success/failure counts

#### Scenario: Storage cleanup warning does not fail the item
- **WHEN** a job order is deleted successfully from the database but deletion of one or more attachment files/images from storage fails
- **THEN** the job order SHALL be treated as deleted for user-facing success/failure counts
- **AND** the storage cleanup failure SHOULD be logged for diagnostics

### Requirement: Job List UI state SHALL refresh after delete completion
The system SHALL refresh rows and selection state after delete execution so the list reflects current server state.

#### Scenario: Successful delete refreshes list and clears selection
- **WHEN** delete execution completes
- **THEN** Job List data SHALL be reloaded and selected ids SHALL be cleared

#### Scenario: Delete request in progress disables repeated actions
- **WHEN** delete execution is running
- **THEN** the Delete action SHALL remain disabled until processing completes

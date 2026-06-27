"# Order Record Add Job Specification

## ADDED Requirements

### Requirement: ADD NEW button emits add-new-job event
The `OrderRecordDialog` SHALL emit an `add-new-job` event when the user clicks the "ADD NEW" button in edit mode.

#### Scenario: User clicks ADD NEW button in edit mode
- **WHEN** the user is viewing an order in edit mode and clicks the "ADD NEW" button
- **THEN** the dialog emits an `add-new-job` event containing the parent order's context (orderId, orderNumber, customerName)

#### Scenario: ADD NEW button visible only in edit mode
- **WHEN** the dialog is in create mode
- **THEN** the "ADD NEW" button is not displayed (the entire jobs section is hidden in create mode)

### Requirement: Parent handler opens JobOrderForm in create mode
The parent view (`OrderListView`) SHALL handle the `add-new-job` event by closing `OrderRecordDialog` and opening `JobOrderForm` in create mode.

#### Scenario: Parent receives add-new-job event
- **WHEN** `OrderRecordDialog` emits the `add-new-job` event
- **THEN** the parent closes `OrderRecordDialog` (`formOpen = false`)
- **AND** opens `JobOrderForm` with a minimal job object that preserves order context

#### Scenario: Job form pre-populated with order defaults
- **WHEN** `JobOrderForm` is opened for creating a new job within an existing order
- **THEN** the form fields are pre-populated with values from the parent order (orderNumber, customerName, orderedBy)

### Requirement: Data refreshes after new job save
After saving a new job via `JobOrderForm`, the parent view SHALL refresh the order list so the new job appears in the related orders table.

#### Scenario: New job saved successfully
- **WHEN** the user saves a new job in `JobOrderForm`
- **THEN** the parent view refreshes the order data from the server
- **AND** the new job appears in the related orders table when `OrderRecordDialog` is reopened
"
"# Fix Order Record ADD NEW Button

## Why

The "ADD NEW" button in `OrderRecordDialog` currently does nothing useful - it only resets the local draft state without creating a new job or providing any visible feedback to the user. This breaks the expected workflow where users want to quickly add a new job to an existing order from within the Order Record dialog.

## What Changes

- **New Event**: Add `add-new-job` event emission from `OrderRecordDialog` when "ADD NEW" is clicked
- **Parent Handler**: Add handler in `OrderListView` (and other parent views) that:
  1. Closes `OrderRecordDialog`
  2. Opens `JobOrderForm` in create mode with order context preserved
  3. After save, refreshes data and optionally reopens `OrderRecordDialog`

### Capabilities

#### New Capabilities
- `order-record-add-job`: Emit event from OrderRecordDialog when ADD NEW is clicked, and handle it in parent views to open JobOrderForm in create mode

#### Modified Capabilities
- None - this is a new capability, not modifying existing spec requirements

### Impact
- **Frontend**: 
  - `ClientApp/src/components/forms/OrderRecordDialog.vue` - add event emission
  - `ClientApp/src/views/OrderListView.vue` - add event handler (primary view using OrderRecordDialog)
  - Other views may need similar handlers if they use OrderRecordDialog
</contents>
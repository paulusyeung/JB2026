"# Tasks: Fix Order Record ADD NEW Button

## 1. OrderRecordDialog - Add Event Emission

- [x] 1.1 Add `add-new-job` to the emit definitions in `OrderRecordDialog.vue`
- [x] 1.2 Replace `resetDraft()` call on "ADD NEW" button with event emission that passes parent order context (orderId, orderNumber, customerName)

## 2. OrderListView - Add Event Handler

- [x] 2.1 Add `@add-new-job` listener to `<OrderRecordDialog>` component in `OrderListView.vue`
- [x] 2.2 Implement `handleAddNewJob(orderContext)` handler that:
  - Closes `OrderRecordDialog` (`formOpen = false`)
  - Sets up `jobFormJob` with minimal job data preserving order context
  - Opens `JobOrderForm` (`jobFormOpen = true`)

## 3. Verification

- [x] 3.1 Test that clicking "ADD NEW" in OrderRecordDialog closes it and opens JobOrderForm
- [x] 3.2 Verify that JobOrderForm is pre-populated with parent order's customerName, orderNumber, and orderedBy
- [x] 3.3 Test that after saving a new job, the data refreshes and the new job appears in the related orders list"
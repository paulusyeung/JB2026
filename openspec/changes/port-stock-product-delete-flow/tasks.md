# Tasks - port-stock-product-delete-flow

## 1. Legacy Parity And Contract Definition

- [x] 1.1 Confirm legacy delete lifecycle details from JB2015 (`retire` first, `hard delete` second) and map expected UI outcomes
- [x] 1.2 Define API response contract to return lifecycle outcome (`retired` or `hardDeleted`) with target product id
- [x] 1.3 Confirm authorization and audit metadata requirements for retire and hard-delete operations

## 2. Backend Delete Lifecycle Implementation

- [x] 2.1 Implement/extend delete endpoint/domain flow to retire non-retired products on first delete request
- [x] 2.2 Implement hard-delete branch for already-retired products
- [x] 2.3 Implement cascading cleanup on hard delete for stock in/out rows and product attachment rows
- [x] 2.4 Implement physical file/image cleanup orchestration and failure handling for hard delete
- [x] 2.5 Return lifecycle-aware delete result payload and domain error codes

## 3. Client Stock Service Integration

- [x] 3.1 Add or update stock service delete method used by both `StockView` and `ProductRecordDialog`
- [x] 3.2 Add lifecycle-aware response/request typing and error mapping in ClientApp API models
- [x] 3.3 Add localized messaging keys for lifecycle-specific delete outcomes and failure states

## 4. StockView Delete Wiring

- [x] 4.1 Replace gated delete placeholder in `StockView.vue` with executable delete flow
- [x] 4.2 Implement delete confirmation for single-selection and checkbox multi-selection contexts
- [x] 4.3 Process checkbox multi-delete through shared service path and present aggregate outcome feedback
- [x] 4.4 Refresh list rows/selection state after delete completion and keep toolbar enablement consistent

## 5. ProductRecordDialog Delete Reuse

- [x] 5.1 Replace current delete action in `ProductRecordDialog.vue` to use shared delete service lifecycle handling
- [x] 5.2 Show confirmation and lifecycle-aware success/failure messages in dialog context
- [x] 5.3 Close dialog and emit parent refresh signals after successful delete

## 6. Tests And Verification

- [x] 6.1 Add frontend tests for `StockView` single delete, checkbox batch delete, and cancellation path
- [x] 6.2 Add frontend tests for `ProductRecordDialog` delete confirmation, success close, and refresh signaling
- [x] 6.3 Add backend parity/correctness tests for retire-first and hard-delete-second behavior
- [x] 6.4 Add backend tests for cascading cleanup of stock in/out rows, attachment rows, and file cleanup failure paths
- [ ] 6.5 Run stock-module regression checks and document parity sign-off evidence for both entry points

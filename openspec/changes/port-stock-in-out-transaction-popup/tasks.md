# Tasks - port-stock-in-out-transaction-popup

## 1. Legacy Parity And Contract Definition

- [x] 1.1 Confirm legacy StockInOut behavior matrix (field defaults, validation text intent, save-confirmation flow)
- [x] 1.2 Define/confirm API contract for creating stock in/out transaction and returning updated balance context
- [x] 1.3 Align authorization and audit requirements (created/modified metadata and user identity propagation)

## 2. Client Service Implementation

- [x] 2.1 Add stock service method for create-stock-in-out transaction request
- [x] 2.2 Add request/response types for stock in/out payload and result handling
- [x] 2.3 Add service-level error mapping for validation and domain failures

## 3. Stock In/Out Dialog Component

- [x] 3.1 Create reusable `StockInOutDialog` component with props/emits for shared invocation
- [x] 3.2 Implement form fields (stock number, date, reference, qty) with legacy-parity defaults
- [x] 3.3 Implement validation rules for existing stock number and signed integer quantity
- [x] 3.4 Implement confirmation flow for Save and Save & Close
- [x] 3.5 Emit success event payload to trigger caller-side data refresh

## 4. Entry-Point Wiring

- [x] 4.1 Wire `StockView` Stock In/Out action to open dialog for selected row context
- [x] 4.2 Enforce selection precondition in `StockView` (single-record transactional context)
- [x] 4.3 Wire `ProductRecordDialog` Stock In/Out action to open shared dialog for active product
- [x] 4.4 Refresh stock list and product movement/history data after successful transaction save

## 5. Backend And Data Integrity

- [x] 5.1 Implement/verify API endpoint to persist stock in/out transaction row
- [x] 5.2 Apply atomic balance update (`balance + qty`) with transaction safety
- [x] 5.3 Ensure server-side parity validation for stock existence and qty numeric constraints
- [x] 5.4 Add regression protection for concurrent updates and error handling paths

## 6. Tests And Acceptance

- [x] 6.1 Add component tests for both launch paths and dialog visibility lifecycle
- [x] 6.2 Add validation tests for missing stock, unknown stock, invalid qty, and signed qty acceptance
- [x] 6.3 Add integration/parity tests for transaction persistence and balance recalculation correctness
- [x] 6.4 Add UI tests for Save & Close success behavior and caller refresh signaling
- [x] 6.5 Run full stock-module regression checks and document parity sign-off evidence

## 1. Legacy Parity Discovery And Contract Definition

- [x] 1.1 Capture a parity checklist from legacy attachment manager behavior (size presets, selection, upload, download, delete, preview)
- [x] 1.2 Define/confirm product attachment API contract for list, upload, delete, and download/preview access
- [x] 1.3 Add or update TypeScript stock attachment DTOs and service method signatures

## 2. Backend And Service Integration

- [x] 2.1 Implement or map API endpoints for product attachment list retrieval by productId
- [x] 2.2 Implement upload endpoint integration with max file size and validation error mapping
- [x] 2.3 Implement delete endpoint integration supporting multi-select deletion workflow
- [x] 2.4 Implement download/preview URL or content endpoint integration for mixed file types

## 3. Shared Attachment Dialog UI

- [x] 3.1 Create StockAttachmentDialog component with product-scoped props and open/close lifecycle handling
- [x] 3.2 Implement attachment list rendering with selectable items and filename metadata
- [x] 3.3 Implement thumbnail size preset controls for small, medium, large, and x-large views
- [x] 3.4 Implement upload action with loading/progress and post-upload list refresh
- [x] 3.5 Implement batch download and batch delete actions with delete confirmation
- [x] 3.6 Implement file-type-aware preview behavior (inline image, PDF best-effort inline, deterministic fallback)

## 4. Caller Integration In Stock Screens

- [x] 4.1 Replace StockView attachment gated action with StockAttachmentDialog launch using selected product context
- [x] 4.2 Replace ProductRecordDialog attachment gated action with shared StockAttachmentDialog launch
- [x] 4.3 Ensure both entry points pass consistent product identity and refresh state after mutations

## 5. Validation, Permissions, And UX Hardening

- [x] 5.1 Enforce selection guards for delete/download actions and user-facing validation messages
- [x] 5.2 Preserve delete permission behavior and block destructive actions when not authorized
- [x] 5.3 Add resilient error handling for upload/delete/preview failures without breaking dialog state

## 6. Testing And Completion Gate

- [x] 6.1 Add frontend component tests for size mode switching, selection, and action enablement
- [x] 6.2 Add frontend integration tests for launch from both StockView and ProductRecordDialog
- [x] 6.3 Add/extend parity or API tests for attachment list/upload/delete/download behavior
- [ ] 6.4 Run targeted test suites and verify no regressions in existing stock workflows
- [ ] 6.5 Perform manual QA for desktop/mobile attachment flows and mixed file type preview fallback

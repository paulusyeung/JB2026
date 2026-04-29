# Proposal - port-stock-attachment-manager-dialog

## Why

The stock flow in ClientApp still gates attachment actions, which prevents users from adding, deleting, and reviewing stock images without returning to the legacy UI. Porting the legacy attachment manager now closes a high-friction parity gap and unlocks end-to-end stock maintenance in the modern interface.

## What Changes

- Add a reusable stock attachment manager dialog in ClientApp that supports:
  - image-size layout options (small, medium, large, x-large)
  - upload attachment
  - download selected attachment(s)
  - delete selected attachment(s) with confirmation
  - preview attachment content (images inline, non-image via viewer/download fallback)
- Integrate this dialog with existing Attachment actions in:
  - StockView toolbar Attachment button
  - ProductRecordDialog Attachment button
- Replace current "action unavailable" behavior for attachment actions with the new dialog flow.
- Add stock attachment API/service operations required by the dialog (list, upload, download, delete, preview metadata/URL as needed).
- Preserve legacy behavior for mixed file types, including PDF preview image handling parity where practical in web UX.

## Capabilities

### New Capabilities
- `stock-attachment-management`: Manage stock record attachments from list and product record contexts, including upload, delete, download, and preview with selectable image-size display modes.

### Modified Capabilities
- None.

## Impact

- Affected frontend code:
  - stock views/components in ClientApp (StockView, ProductRecordDialog, new attachment dialog/viewer components)
  - stock service client layer and types
- Affected backend/API surface:
  - stock attachment endpoints and file transport contracts (if not already present)
- Affected testing:
  - frontend component/integration tests for attachment workflows
  - API/parity tests for stock attachment CRUD/preview behavior
- User impact:
  - attachment workflows become available directly in modern stock screens, reducing dependency on legacy controls.

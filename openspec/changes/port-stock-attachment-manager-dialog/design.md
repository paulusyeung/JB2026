# Design - port-stock-attachment-manager-dialog

## Context

The current ClientApp stock flow exposes Attachment actions in both StockView and ProductRecordDialog, but both actions are currently gated and do not execute attachment workflows. In legacy Job.Book, AttachmentManager provides an operational attachment workspace with image-size views, upload/download/delete commands, and file preview behavior for image and non-image files.

This change ports that capability into Vue 3 + Vuetify and reuses one dialog implementation from two entry points:
- stock list context (StockView)
- product record context (ProductRecordDialog)

The implementation must preserve core user expectations from legacy while using browser-native file and preview interaction patterns.

## Goals / Non-Goals

**Goals:**
- Provide one reusable stock attachment dialog that supports list/select/upload/download/delete/preview.
- Preserve legacy image-size modes (small, medium, large, x-large) and selectable thumbnail browsing.
- Support mixed file types with practical parity:
  - image thumbnails and inline preview
  - PDF preview support when possible and safe fallback behavior when not
- Wire both existing Attachment buttons to open the same dialog for the active stock product.
- Define API/service contracts for attachment list and mutations with parity-oriented test coverage.

**Non-Goals:**
- Pixel-perfect replication of legacy WinForms/VWG control visuals.
- Reintroducing retired Google Docs synchronization behavior from legacy code.
- Implementing unrelated stock dialog features (print/export/advanced media editing).

## Decisions

### 1) Single reusable dialog component shared by both stock contexts
- Decision: build a shared component (for example, StockAttachmentDialog) and open it from StockView and ProductRecordDialog.
- Rationale: prevents duplicate attachment logic and keeps behavior consistent across both entry points.
- Alternative considered: separate context-specific dialogs.
- Why not chosen: would duplicate list selection, upload/download/delete, and preview logic.

### 2) Attachment operations modeled as product-scoped service calls
- Decision: add/extend stock service methods around a product-scoped attachment resource.
- Rationale: the modern stock flow already keys behavior by product identity; product-scoped contracts simplify caller integration and permission checks.
- Alternative considered: generic attachment endpoint keyed by stock number string only.
- Why not chosen: increases client-side lookup/parsing complexity and weakens type-level linkage to product records.

### 3) Browser-native preview/download strategy with file-type-aware rendering
- Decision: render image files directly in the dialog preview pane and open/download non-image files via signed URL or content endpoint.
- Rationale: this matches web platform constraints while preserving the legacy intent of quick preview and access.
- Alternative considered: generate and persist preview thumbnails for every file type in frontend.
- Why not chosen: high complexity with limited value and browser security constraints.

### 4) Legacy size presets retained as view-density controls
- Decision: keep four presets (small, medium, large, x-large) and map them to responsive thumbnail dimensions.
- Rationale: users explicitly rely on these display densities in legacy.
- Alternative considered: a free-form slider.
- Why not chosen: less parity and higher UX variance across devices.

### 5) Multi-select actions with explicit confirmation for destructive operations
- Decision: allow selecting multiple attachments and require confirmation before delete.
- Rationale: matches legacy behavior and reduces accidental data loss.
- Alternative considered: immediate delete-on-click.
- Why not chosen: not parity-compatible and higher risk.

### 6) Product-scoped attachment API contract
- Decision: standardize attachment CRUD around product-scoped routes under `/api/v2/stock/products/{productId}`.
- Contract:
  - `GET /attachments` -> list attachment metadata for a product.
  - `POST /attachments` -> multipart upload (`files[]`) with max-size validation.
  - `DELETE /attachments` -> batch delete by `attachmentIds` payload.
  - `GET /attachments/{attachmentId}` -> download or inline preview stream (`inline=true`).
- Rationale: keeps client wiring predictable from both StockView and ProductRecordDialog, while preserving parity behaviors (batch actions + preview fallback).
- Trade-off: URL-based preview/download for each item introduces one request per file for batch download, accepted for initial parity delivery.

## Risks / Trade-offs

- [Risk] Backend attachment APIs may be incomplete or inconsistent across environments.
  → Mitigation: define the required contract in this change and add parity/API tests before UI finalization.

- [Risk] Inline preview behavior differs by browser MIME handling.
  → Mitigation: support best-effort inline preview for known image/PDF types and deterministic download fallback.

- [Risk] Large file uploads can degrade dialog responsiveness.
  → Mitigation: show upload progress/loading states, enforce configured max file size, and re-fetch list after completion.

- [Risk] Existing stock views may diverge in how active product identity is resolved.
  → Mitigation: standardize dialog props as productId + stockNumber and validate on open.

## Migration Plan

1. Add/verify stock attachment backend contracts and service client methods.
2. Implement shared attachment dialog and viewer internals behind feature branch.
3. Replace gated Attachment button handlers in StockView and ProductRecordDialog with dialog launch logic.
4. Add/extend tests (frontend and parity/API) for list, upload, delete, download, and preview fallbacks.
5. Roll out with existing stock feature set; if blocking issues occur, revert only the new dialog wiring and keep prior gated behavior as rollback path.

## Open Questions

- Should delete be soft-delete (metadata flag) or hard-delete (file + metadata) for product attachments in JB2026?
- Which exact endpoint shape will be authoritative for preview/download URLs in API migration phase?
- Do we require server-side generated PDF thumbnail images for parity, or is inline PDF preview/download sufficient for the first release?

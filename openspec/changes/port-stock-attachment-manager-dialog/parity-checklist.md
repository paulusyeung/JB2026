# Stock Attachment Legacy Parity Checklist

## Coverage Matrix

- [x] Size presets: `small`, `medium`, `large`, `x-large` exposed as deterministic density modes.
- [x] Selection model: multi-select via tile-level checkbox state and guarded batch actions.
- [x] Upload workflow: one-or-more files, max-size validation, immediate list refresh after success.
- [x] Download workflow: per-item download plus batch download preserving original filenames.
- [x] Delete workflow: multi-select delete with explicit confirmation and post-delete refresh.
- [x] Preview workflow:
  - [x] Image types (`png`, `jpg`, `jpeg`, `gif`, `webp`, `bmp`, `svg`) inline preview.
  - [x] PDF best-effort inline open in browser tab.
  - [x] Other file types deterministic file download fallback.

## API Contract Snapshot

- `GET /api/v2/stock/products/{productId}/attachments`
  - Returns attachment list with ids, filename metadata, size, and disk-availability flags.
- `POST /api/v2/stock/products/{productId}/attachments`
  - Multipart upload (`files[]`), validates empty/oversized files, persists file and metadata.
- `DELETE /api/v2/stock/products/{productId}/attachments`
  - Body: `{ attachmentIds: Guid[] }`, supports multi-select delete with aggregate result.
- `GET /api/v2/stock/products/{productId}/attachments/{attachmentId}`
  - Returns content stream for download/preview (`inline=true` for inline content disposition).

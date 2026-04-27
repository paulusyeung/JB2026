# QuestPDF Developer Guide

## Scope

This guide documents the reporting structure introduced for stock print PDF generation and how to add new reports safely.

## Project layout

- Shared reporting building blocks are in `JB2026.Reporting`.
- Report-specific documents stay in `JB2026.Api` when they depend on API-only models.

## Startup requirements

Initialize QuestPDF before any rendering call:

1. Set license:
   - `QuestPDF.Settings.License = LicenseType.Community`
2. Initialize embedded fonts once:
   - `FontRegistry.EnsureInitialized()`

Both are configured in `JB2026.Api/Program.cs`.

## Shared components

- `FontRegistry`
  - Registers embedded font resources used across reports.
  - Initialization is idempotent and safe to call more than once.
- `PageLayout`
  - Holds standard page dimensions and margins.
  - Current stock print dimensions match legacy media box (`842 x 1191`).
- `ReportTable`
  - Reusable table component with:
    - Header styling
    - Alternating row backgrounds
    - Per-cell alignment
    - Optional CJK fallback style flag
- `DocumentBase<TModel>`
  - Common document base for metadata and text-style helpers.

## Stock print flow

1. `StockProductPrintComposer` builds `StockProductPrintDocument`.
2. `StockProductPdfRenderer` creates `StockPrintDocument`.
3. `StockPrintDocument.GeneratePdf()` returns PDF bytes.

## Implementing a new report

1. Add or reuse a model in the API layer.
2. Create a dedicated document class implementing `IDocument` (or extending `DocumentBase<TModel>`).
3. Define:
   - `GetMetadata()` for title/author
   - `Compose()` for layout
4. Use `PageLayout` and `ReportTable` for consistency.
5. Use CJK fallback style for fields that may contain multilingual content.
6. Add renderer wiring behind a stable interface consumed by controller endpoints.

## Testing guidance

- Use PDF text extraction (PdfPig) rather than raw byte string matching.
- Validate at minimum:
  - Metadata
  - Required section labels and key values
  - Deterministic row ordering and numbering
  - CJK text extraction success
  - Font registry idempotent initialization

## Maintenance notes

- Do not reintroduce `StockPrint:FontName` style configuration keys.
- Keep fonts embedded to avoid environment-specific rendering drift.
- If replacing fonts, update `FontRegistry` resource names and verify CJK fixtures again.

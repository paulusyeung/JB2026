## Why

The current stock print PDF renderer is a hand-rolled PDF 1.4 builder that manually constructs content streams, font objects, and cross-reference tables. This approach is fragile, difficult to maintain, and hard to extend for future reports. As the project migrates more legacy reports to the modern API, a sustainable, declarative PDF rendering foundation is needed to support multiple report types without accumulating technical debt.

## What Changes

- **Replace** the hand-rolled `StockProductPdfRenderer` with a QuestPDF-based implementation using a declarative C# DSL.
- **Introduce** a shared reporting infrastructure layer (`JB2026.Reporting`) with common building blocks: font registry, page layout conventions, table components, and header/footer patterns.
- **Migrate** the existing stock record print report to use the new QuestPDF renderer as the first production report.
- **Establish** a reusable report composition pattern (composer → document model → renderer) that future reports can follow.
- **Add** embedded font support: Noto Sans SC for CJK text, and an open-source Latin font (e.g., Lato or Inter) to replace the legacy Helvetica/STSong-Light dependencies. QuestPDF requires embedded TTF/OTF font files — PostScript Type 1 names like "Helvetica" are not supported.
- **Remove** the `StockPrint:FontName` configuration key as font handling becomes code-based.
- **Rewrite** parity tests to use a PDF text extraction library (e.g., PdfPig) since QuestPDF generates proper binary PDF content that cannot be string-searched like the hand-rolled output.
- **Configure** QuestPDF Community MIT license at startup (`QuestPDF.Settings.License = LicenseType.Community`).

## Capabilities

### New Capabilities
- `questpdf-reporting-infrastructure`: Shared QuestPDF-based reporting foundation with font registry, page layout conventions, table components, and renderer abstraction for all future reports.
- `report-font-management`: Centralized font embedding and fallback strategy supporting Latin and CJK (Chinese) text across all reports.

### Modified Capabilities
- `stock-record-print-pdf`: The stock record PDF report will use the new QuestPDF renderer instead of the hand-rolled PDF builder. The output structure and content remain the same, but the rendering engine changes.

## Impact

- **Affected backend**:
  - `JB2026.Api/Services/StockProductPdfRenderer.cs` — replaced entirely with QuestPDF implementation
  - `JB2026.Api/Services/IStockProductPdfRenderer.cs` — interface stays, implementation changes
  - `JB2026.Api/Controllers/StockController.cs` — no changes (uses interface)
  - `JB2026.Api/Program.cs` — DI registration stays, adds QuestPDF license configuration and font initialization
  - `JB2026.Api/JB2026.Api.csproj` — adds QuestPDF NuGet package reference, adds project reference to `JB2026.Reporting`
  - New project: `JB2026.Reporting` (shared library for report building blocks — fonts, layout, table components)
  - `JB2026.sln` — add `JB2026.Reporting` project to solution
- **Affected tests**:
  - `JB2026.Api.ParityTests/StockPrintControllerTests.cs` — **requires complete rewrite**: current tests parse raw PDF bytes as UTF-8 strings and search for literal text/hex patterns. QuestPDF generates binary PDF content that is not parseable this way. Tests must use a PDF text extraction library (e.g., PdfPig) to validate content.
  - `JB2026.Api.ParityTests/JB2026.Api.ParityTests.csproj` — adds PdfPig (or equivalent) NuGet package for PDF text extraction in tests
  - `JB2026.WebApp/ClientApp/tests/stock.product-record.spec.ts` — no changes (tests HTTP contract, not PDF internals)
- **Affected frontend**: None (PDF is still delivered as `application/pdf` blob)
- **Dependencies**: Adds `QuestPDF` NuGet package, adds PdfPig (or equivalent) to test project, embeds Noto Sans SC and Latin TTF font files
- **Operational impact**: PDF generation becomes more maintainable and extensible; initial layout may need visual parity validation against legacy output

## Design Notes

- **Page size**: The legacy renderer uses `MediaBox [0 0 842 1191]` which is 842×1191 points — larger than standard A4 (595×842). This needs a deliberate decision: match the legacy page size exactly, or switch to standard A4. Either way, the design and specs should reflect the chosen size.
- **Data model dependency**: The `StockProductPrintDocument` data model lives in `JB2026.Api.Models`. The QuestPDF `IDocument` implementation (`StockPrintDocument`) depends on this model. To avoid `JB2026.Reporting` depending on `JB2026.Api`, the QuestPDF document implementation should stay in `JB2026.Api`, while `JB2026.Reporting` provides only shared building blocks (font registry, page layout, table components).
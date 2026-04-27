## Context

The current `StockProductPdfRenderer` is a ~400-line hand-rolled PDF 1.4 builder that manually constructs:
- PDF object directories with cross-reference tables
- Content streams with raw PostScript-like operators (`BT`, `ET`, `Tj`, `re`, `f`, `S`)
- Font dictionaries for Latin (Helvetica/Helvetica-Bold), CJK (STSong-Light via CIDFont), and monospace (Courier)
- Page-level pagination logic for movement history tables
- Text positioning via decimal coordinates with manual width measurements

This works but is extremely fragile: any layout change requires understanding PDF internals, adding new reports means duplicating this complexity, and debugging rendering issues requires PDF hex dumps.

The project has already established a composer/renderer pattern (`IStockProductPrintComposer` → `StockProductPrintDocument` → `IStockProductPdfRenderer`) which is architecturally sound. The renderer implementation just needs to change.

Future reports (job order PDFs, invoice PDFs, quotation PDFs) will need the same PDF generation capability. A shared reporting foundation will prevent each report from reinventing font handling, page layouts, and table rendering.

## Goals / Non-Goals

**Goals:**
- Replace the hand-rolled PDF renderer with QuestPDF for the stock record print report.
- Establish a shared reporting infrastructure that future reports can reuse (font registry, page conventions, table components).
- Maintain content parity with the existing PDF output (same data, same ordering, same structure).
- Support CJK text rendering with open-source embedded TTF fonts (Noto Sans SC).
- Keep the existing `IStockProductPdfRenderer` interface contract so the controller requires no changes.
- Ensure the rendering pipeline is testable (unit tests can verify document content via PDF text extraction).

**Non-Goals:**
- Migrate all legacy reports in this change (only stock record print is in scope).
- Introduce a visual report designer or template editor.
- Add asynchronous report generation or job queues.
- Change the stock record data composition logic (`StockProductPrintComposer` stays as-is).
- Modify the frontend print button behavior or API contract.
- Achieve pixel-perfect visual parity with the legacy PDF (content parity is the goal, not layout parity).

## Decisions

### 1. QuestPDF as the PDF rendering engine

**Decision:** Use QuestPDF (Community MIT edition) as the PDF rendering engine.

**Rationale:**
- Declarative C# DSL makes layouts readable and maintainable
- Built-in table components with automatic pagination
- Active maintenance, .NET 8 compatible
- Community edition is genuine MIT license — free for all use including commercial
- Supports custom TTF/OTF font embedding for CJK support
- No separate template language to learn

**Configuration requirement:** QuestPDF requires explicit license configuration at startup:
```csharp
QuestPDF.Settings.License = LicenseType.Community;
```
Without this, QuestPDF throws an exception. This must be called before any document generation.

**Alternatives considered:**
- **iText 7**: Powerful but complex API, AGPL/commercial licensing concerns
- **PdfSharp**: Mature but limited table/pagination support, less active development
- **DinkToPdf (WebKit)**: Requires browser engine dependency, heavier deployment footprint
- **Keep hand-rolled**: Rejected due to maintenance burden and inability to scale to multiple reports

### 2. Shared `JB2026.Reporting` library — building blocks only

**Decision:** Create a new class library `JB2026.Reporting` for shared report building blocks. Report-specific `IDocument` implementations stay in `JB2026.Api`.

**Rationale:**
- Centralizes font registry, page layout conventions, and common table components
- Future reports (job orders, invoices, quotations) can reuse these building blocks
- Avoids circular dependency: `StockProductPrintDocument` (the data model) lives in `JB2026.Api.Models`, so any QuestPDF `IDocument` that consumes it must also live in `JB2026.Api`
- `JB2026.Reporting` stays dependency-free of `JB2026.Api`, keeping it truly reusable

**Contents of `JB2026.Reporting`:**
- `FontRegistry` — static font registration at startup (Latin + CJK)
- `PageLayout` — page size, margins, header/footer conventions
- `ReportTable` — reusable table component with header styling, alternating row colors
- `DocumentBase<T>` — base class for report documents with common metadata
- Embedded font files (TTF)

**Dependency direction:**
```
JB2026.Api ──references──▶ JB2026.Reporting
                           (shared building blocks)

JB2026.Api contains:
  - StockPrintDocument (IDocument, uses data model + Reporting building blocks)
  - StockProductPdfRenderer (creates StockPrintDocument, generates PDF)
  - StockProductPrintDocument (data model, stays in Api.Models)
```

**Alternatives considered:**
- **Put IDocument impls in JB2026.Reporting**: Rejected because they depend on data models in `JB2026.Api.Models`, creating a circular dependency
- **Move data models to JB2026.Reporting**: Rejected because the models are API-specific DTOs, not reporting concerns
- **Folder in JB2026.Api**: Rejected because it doesn't encourage reuse of building blocks across potential future API projects

### 3. Noto Sans SC for CJK + open-source Latin font

**Decision:** Embed Noto Sans SC (Apache 2.0) for CJK text and an open-source Latin font (Lato, Inter, or similar) for Latin text. Replace both the legacy Helvetica (Type 1) and STSong-Light references.

**Rationale:**
- QuestPDF requires embedded TTF/OTF font files — PostScript Type 1 names like "Helvetica" are not supported
- Noto Sans SC: open-source (Apache 2.0), comprehensive Chinese character coverage
- Lato/Inter: open-source Helvetica-style alternatives, small file size (~200KB)
- All fonts embedded as assembly resources, no system font dependency

**Alternatives considered:**
- **Helvetica (current)**: Not available as embeddable TTF without licensing — QuestPDF cannot use Type 1 font names
- **STSong-Light (current)**: Adobe-licensed, not redistributable
- **System fonts**: Unreliable across deployment environments (Docker, CI, etc.)
- **Source Han Sans**: Similar quality to Noto Sans SC but larger file size

### 4. Document definition pattern per report

**Decision:** Each report defines its own `IDocument` implementation using QuestPDF's composition pattern. These implementations live in `JB2026.Api` (not `JB2026.Reporting`) because they depend on API-specific data models.

**Rationale:**
- Type-safe document composition
- Each report self-contained in one class
- Easy to add/remove reports without affecting others
- Testable via QuestPDF's preview API

**Example structure:**
```csharp
// Lives in JB2026.Api, not JB2026.Reporting
public class StockPrintDocument : IDocument
{
    private readonly StockProductPrintDocument _data;
    public StockPrintDocument(StockProductPrintDocument data) => _data = data;
    // GetMetadata(), Compose()
}
```

### 5. Keep existing interface contract

**Decision:** `IStockProductPdfRenderer.Render(StockProductPrintDocument)` stays unchanged.

**Rationale:**
- Zero changes to `StockController`
- Clear separation between data composition and rendering
- Existing test structure (controller → composer → renderer → assert) remains valid

### 6. Page size — match legacy dimensions

**Decision:** Use the legacy page dimensions (842×1191 points) rather than standard A4 (595×842).

**Rationale:**
- The legacy renderer uses `MediaBox [0 0 842 1191]` — this is larger than A4
- Switching to A4 would change the layout significantly (less space, different pagination)
- Content parity is easier to achieve with the same page dimensions
- Can revisit page size in a future change if needed

**Note:** This should be validated — if the legacy page size was accidental (e.g., a miscalculation), switching to A4 may be preferable. This is an open question for the team.

### 7. Parity test rewrite strategy

**Decision:** Rewrite parity tests using a PDF text extraction library (PdfPig) instead of raw byte string searches.

**Rationale:**
- The current tests parse raw PDF bytes as UTF-8 strings and search for literal text (e.g., `Assert.Contains("Stock Number:", content)`). This only works because the hand-rolled renderer produces semi-readable PDF content.
- QuestPDF generates proper binary PDF with compressed content streams, font subsetting, and internal encoding. Raw string searches will not work.
- The CJK test currently checks for UTF-16 hex-encoded strings matching the hand-rolled `<hex> Tj` operator pattern — completely QuestPDF-incompatible.
- PdfPig (Apache 2.0) can extract text from PDF pages, enabling content validation without depending on internal PDF structure.

**New test approach:**
```csharp
// Before (hand-rolled PDF):
var content = Encoding.UTF8.GetString(file.FileContents);
Assert.Contains("Stock Number:", content);

// After (QuestPDF + PdfPig):
using var pdfDocument = PdfDocument.Open(file.FileContents);
var pageText = string.Join(" ", pdfDocument.GetPages().Select(p => p.Text));
Assert.Contains("Stock Number:", pageText);
```

## Risks / Trade-offs

- **[Parity tests require complete rewrite]** → Mitigation: Use PdfPig for PDF text extraction. Tests validate the same content assertions but via extracted text instead of raw bytes. This is actually more robust — tests won't break if internal PDF structure changes.

- **[Layout drift from legacy PDF]** → Mitigation: Visual parity validation against representative legacy samples. The content (data, ordering, structure) remains identical; only the rendering engine changes. Exact pixel-level parity is a non-goal.

- **[QuestPDF community license]** → Mitigation: QuestPDF Community edition is genuine MIT license. No revenue thresholds, no restrictions. JB2026 is an internal ERP system. If licensing ever becomes a concern, the interface abstraction allows swapping to another renderer.

- **[Font file size increases deployment]** → Mitigation: Noto Sans SC is ~15MB uncompressed but can be subsetted to ~5MB for commonly used characters. Latin font adds ~200KB. Fonts are embedded once at startup, not per-request.

- **[QuestPDF rendering performance]** → Mitigation: Stock record reports typically have <100 movement rows. QuestPDF handles this easily. If future reports have thousands of rows, pagination and streaming can be optimized.

## Migration Plan

1. **Add QuestPDF NuGet package** to `JB2026.Api`
2. **Create `JB2026.Reporting` library** and add to `JB2026.sln`
3. **Add project reference** from `JB2026.Api` to `JB2026.Reporting`
4. **Embed fonts** (Noto Sans SC + Latin TTF) in the reporting library
5. **Implement shared building blocks** in `JB2026.Reporting` (FontRegistry, PageLayout, ReportTable)
6. **Configure QuestPDF license** (`LicenseType.Community`) in `Program.cs`
7. **Initialize font registry** in `Program.cs` at application startup
8. **Implement `StockPrintDocument`** (QuestPDF `IDocument`) in `JB2026.Api`
9. **Replace `StockProductPdfRenderer`** to use QuestPDF via `StockPrintDocument`
10. **Add PdfPig** to test project for PDF text extraction
11. **Rewrite parity tests** to use PdfPig text extraction instead of raw byte parsing
12. **Visual parity validation** against legacy PDF samples
13. **Remove `StockPrint:FontName` config key** (no longer needed)
14. **Remove old hand-rolled PDF renderer code**

**Rollback strategy:** The old `StockProductPdfRenderer` can be kept as a backup during transition. If QuestPDF has critical issues, the DI registration can point back to the old implementation.

## Open Questions

- Should we subset the Noto Sans SC font to reduce size, or embed the full font?
- Which open-source Latin font should replace Helvetica? (Lato, Inter, or QuestPDF default?)
- Do we need a QuestPDF document preview endpoint for development/debugging?
- Should the reporting library be a separate NuGet package or stay as a project reference?
- Page size: keep legacy 842×1191 dimensions, or switch to standard A4 (595×842)?
- Are there other reports already planned that should be included in this migration?

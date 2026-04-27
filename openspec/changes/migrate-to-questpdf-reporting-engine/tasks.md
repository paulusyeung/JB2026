## 1. Project Setup

- [x] 1.1 Add QuestPDF NuGet package to `JB2026.Api/JB2026.Api.csproj`
- [x] 1.2 Create `JB2026.Reporting` class library project
- [x] 1.3 Add `JB2026.Reporting` project to `JB2026.sln`
- [x] 1.4 Add project reference from `JB2026.Api` to `JB2026.Reporting`
- [x] 1.5 Download and embed Noto Sans SC TTF font file in `JB2026.Reporting`
- [x] 1.6 Download and embed open-source Latin TTF font (e.g., Lato or Inter) in `JB2026.Reporting`
- [x] 1.7 Configure font files as Embedded Resource in the Reporting project

## 2. Shared Reporting Infrastructure

- [x] 2.1 Implement `FontRegistry` class with static initialization for embedded Latin and CJK TTF fonts
- [x] 2.2 Add `PageLayout` conventions (page size matching legacy 842×1191, margins, header/footer defaults)
- [x] 2.3 Create `ReportTable` reusable component with header styling and alternating rows
- [x] 2.4 Create `DocumentBase<T>` abstract class for common document metadata
- [x] 2.5 Configure QuestPDF license in `Program.cs` (`QuestPDF.Settings.License = LicenseType.Community`)
- [x] 2.6 Register font initialization in `Program.cs` at application startup

## 3. Stock Print Document Implementation

- [x] 3.1 Create `StockPrintDocument` class implementing QuestPDF's `IDocument` in `JB2026.Api` (not in Reporting library, since it depends on `StockProductPrintDocument` data model)
- [x] 3.2 Implement `GetMetadata()` with report title and author
- [x] 3.3 Implement page setup (matching legacy page dimensions, margins, header/footer with page numbers)
- [x] 3.4 Implement header section with product identity fields (stock number, code, name)
- [x] 3.5 Implement summary section (production info, remarks, MOQ, balance)
- [x] 3.6 Implement movement history table with all required columns
- [x] 3.7 Add deterministic row ordering and numbering logic (ascending by InOutDate, then ascending by ModifiedOn)
- [x] 3.8 Apply CJK font fallback to text elements that may contain Chinese characters

## 4. Renderer Replacement

- [x] 4.1 Replace `StockProductPdfRenderer` implementation to use QuestPDF
- [x] 4.2 Update constructor to remove `IConfiguration` dependency
- [x] 4.3 Implement `Render()` method using `StockPrintDocument` and QuestPDF document generation
- [x] 4.4 Update `Program.cs` DI registration if needed
- [x] 4.5 Remove `StockPrint:FontName` configuration key usage

## 5. Testing

- [x] 5.1 Add PdfPig (or equivalent PDF text extraction library) NuGet package to `JB2026.Api.ParityTests`
- [x] 5.2 Rewrite `PrintProductRecord_ReturnsPdfFile_ForExistingProduct` to validate PDF content type and non-empty bytes
- [x] 5.3 Rewrite `PrintProductRecord_IncludesRequiredSections_AndDeterministicOrdering` to use PdfPig text extraction instead of raw UTF-8 string search
- [x] 5.4 Rewrite `PrintProductRecord_PreservesMultilingualTextBytes` to extract text from PDF and check for actual CJK characters instead of searching for UTF-16 hex patterns
- [x] 5.5 Update `CreateController` helper to remove `IConfiguration` dependency from renderer construction
- [x] 5.6 Add unit test for `StockPrintDocument` metadata
- [x] 5.7 Add unit test for movement row ordering and numbering
- [x] 5.8 Add CJK rendering test with Chinese character fixtures
- [x] 5.9 Add font registry initialization test
- [ ] 5.10 Verify frontend print tests still pass (no contract changes)

## 6. Cleanup and Validation

- [x] 6.1 Remove old hand-rolled PDF renderer code
- [x] 6.2 Remove `StockPrint:FontName` from configuration files
- [ ] 6.3 Visual parity validation against legacy PDF samples
- [x] 6.4 Update documentation with QuestPDF usage guidelines
- [x] 6.5 Add developer guide for creating new reports with QuestPDF

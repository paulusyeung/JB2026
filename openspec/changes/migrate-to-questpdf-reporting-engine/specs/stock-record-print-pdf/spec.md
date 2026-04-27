## MODIFIED Requirements

### Requirement: Product record print action MUST generate stock report PDF
The system MUST provide a print operation from the Product Record dialog that generates a PDF for the selected product record and returns it as a binary response with a PDF content type. The rendering engine SHALL be QuestPDF.

#### Scenario: Print from edit mode returns PDF
- **WHEN** a user opens Product Record dialog in edit mode and clicks Print
- **THEN** the client SHALL request a print endpoint for the active product
- **THEN** the server SHALL respond with `application/pdf` content for that product
- **THEN** the PDF SHALL be generated using QuestPDF

### Requirement: PDF report layout MUST include legacy-compatible stock record sections
The generated PDF MUST include product identity fields (stock number, product code, product name), production info, remarks, MQ and balance summary values, and a movement-history table with row number, date, reference, quantity, running balance, modified-on, and modified-by columns. The layout SHALL use QuestPDF's declarative DSL.

#### Scenario: Report contains required sections and columns
- **WHEN** a print request succeeds for a product with movement history
- **THEN** the rendered report SHALL contain all required header/summary fields
- **THEN** the report SHALL contain the movement table with the defined columns
- **THEN** the layout SHALL be defined using QuestPDF's fluent API

### Requirement: Movement rows MUST be ordered and numbered deterministically
The report generator MUST sort movement rows by in/out date-time ascending and then modified-on date-time ascending, and MUST assign row numbers starting at 1 after sorting. This matches the existing `StockProductPrintComposer` behavior where running balance is calculated in chronological order.

#### Scenario: Equal movement dates still produce stable order
- **WHEN** two movement rows have equal in/out date-time but different modified-on values
- **THEN** the row with earlier modified-on SHALL appear first
- **THEN** row numbers SHALL be sequential in rendered order

### Requirement: Print failures MUST be surfaced clearly to users and operators
If PDF generation fails, the client MUST show a user-visible localized error message and the backend MUST log failure context that includes product identifier and failure reason.

#### Scenario: Renderer throws during print generation
- **WHEN** PDF rendering fails for a print request
- **THEN** the API SHALL return a failure response without partial/corrupt PDF output
- **THEN** the client SHALL display a localized print failure message in the dialog
- **THEN** server logs SHALL include traceable context for diagnostics

### Requirement: Print output MUST support multilingual content rendering
The PDF renderer MUST correctly render multilingual text used in product fields (including CJK characters) without replacing characters with missing-glyph placeholders. CJK rendering SHALL use embedded Noto Sans SC font.

#### Scenario: Product name includes CJK characters
- **WHEN** a product record contains CJK text in product name or remarks
- **THEN** the generated PDF SHALL display those characters legibly in the output
- **THEN** the characters SHALL be rendered using the embedded Noto Sans SC font

## ADDED Requirements

### Requirement: Stock print document SHALL use QuestPDF IDocument pattern
The stock record print report SHALL define a dedicated `StockPrintDocument` class implementing QuestPDF's `IDocument` interface.

#### Scenario: Document implements IDocument
- **WHEN** the stock print report is generated
- **THEN** a `StockPrintDocument` class SHALL implement `IDocument`
- **THEN** the document SHALL receive `StockProductPrintDocument` as constructor parameter
- **THEN** the document SHALL define metadata (title, author) via `GetMetadata()`
- **THEN** the document SHALL define layout via `Compose()`

### Requirement: Stock print renderer SHALL use shared reporting infrastructure
The `StockProductPdfRenderer` SHALL use components from the `JB2026.Reporting` library for font registry and page layout conventions.

#### Scenario: Renderer uses shared components
- **WHEN** the stock print renderer generates a PDF
- **THEN** it SHALL use the shared font registry for text rendering
- **THEN** it SHALL use shared page layout conventions (A4, margins)
- **THEN** it SHALL use shared table components for the movement history

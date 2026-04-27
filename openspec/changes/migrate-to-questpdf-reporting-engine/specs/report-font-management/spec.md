## ADDED Requirements

### Requirement: CJK text SHALL render correctly in all reports
The system SHALL correctly render Chinese characters (both Simplified and Traditional) in all PDF reports without missing glyph placeholders (tofu characters).

#### Scenario: Product name contains Chinese characters
- **WHEN** a product record has a name containing Chinese characters (e.g., "產品名稱")
- **THEN** the generated PDF SHALL display the characters legibly
- **THEN** no missing glyph placeholders SHALL appear in the output

#### Scenario: Remarks contain mixed Latin and CJK text
- **WHEN** a product record has remarks with mixed text (e.g., "Item A - 項目A")
- **THEN** both Latin and CJK characters SHALL render correctly
- **THEN** the text SHALL maintain proper spacing and alignment

### Requirement: Font fallback SHALL handle unsupported characters gracefully
When text contains characters not supported by the primary font, the system SHALL fall back to a font that supports those characters rather than displaying missing glyphs.

#### Scenario: Text contains special symbols
- **WHEN** a product field contains special symbols (e.g., currency symbols, arrows)
- **THEN** the system SHALL render the symbols using an appropriate fallback font
- **THEN** no missing glyph placeholders SHALL appear

### Requirement: Font embedding SHALL be self-contained
All fonts used in PDF generation SHALL be embedded in the application deployment. The system SHALL NOT depend on system-installed fonts.

#### Scenario: Deployment to clean environment
- **WHEN** the application is deployed to an environment without pre-installed fonts
- **THEN** PDF generation SHALL still produce correct output
- **THEN** all text SHALL render with the embedded fonts

### Requirement: Font configuration SHALL be code-based
Font registration and configuration SHALL be defined in code rather than external configuration files. The `StockPrint:FontName` configuration key SHALL be removed.

#### Scenario: Font registration in code
- **WHEN** the application starts
- **THEN** fonts SHALL be registered programmatically via QuestPDF's font API
- **THEN** no configuration keys SHALL be required for font selection

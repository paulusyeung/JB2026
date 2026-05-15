## ADDED Requirements

### Requirement: Pivot screens provide actionable summary on phone
On phone layouts, pivot analytics views (`JobStatsView`, `SmlInvoiceStatsView`, and `SmlRtfStatsView` when applicable) SHALL render summary metrics above the pivot/table that act as entry points—not static read-only totals only.

#### Scenario: Summary tile applies a filter
- **WHEN** the user taps a summary tile configured as a filter entry (e.g., row count or amount bucket) on phone layout
- **THEN** the view applies the corresponding filter or dimension preset to the pivot dataset

#### Scenario: Summary remains visible while scrolling pivot
- **WHEN** the user scrolls the pivot container horizontally on phone
- **THEN** the summary card remains accessible above the pivot region

### Requirement: Pivot phone layout does not require desktop-preferred notice when summary is present
When actionable summary and contained horizontal pivot scrolling are present, pivot analytics views SHALL NOT show the desktop-preferred info alert on narrow phones.

#### Scenario: Job stats phone view without desktop-preferred banner
- **WHEN** the user opens Job Stats at or below the `sm` breakpoint
- **THEN** no desktop-preferred pivot notice is displayed
- **AND** the summary card and pivot region are both visible

#### Scenario: Invoice stats phone view without desktop-preferred banner
- **WHEN** the user opens SML Invoice Stats at or below the `sm` breakpoint
- **THEN** no desktop-preferred pivot notice is displayed

### Requirement: Pivot remains horizontally scrollable on phone
The pivot or wide table region SHALL remain inside a contained horizontally scrollable wrapper on phone rather than forcing full table shrink.

#### Scenario: Horizontal scroll for wide pivot
- **WHEN** pivot columns exceed viewport width on phone
- **THEN** the user can scroll the pivot region horizontally without page-level overflow

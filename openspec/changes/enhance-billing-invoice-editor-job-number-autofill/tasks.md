## 1. Backend lookup contract
- [ ] 1.1 Add a billing-focused batch lookup path for canonical job numbers that returns the billing editor fields needed for autofill, including purchaseOrder and productDetails inputs, without widening the generic job-list response.
- [ ] 1.2 Extend API DTOs to distinguish between: Resolved, Unresolved, and Resolved-but-Missing-Section-1.
- [ ] 1.3 Add API tests covering successful multi-job lookup, unresolved job numbers, duplicate canonical job requests, and zero-padded stored job suffixes matching canonical input.

## 2. Description extraction logic
- [ ] 2.1 Implement plain-text normalization and section-1 extraction for Product Details, excluding the section header line and stopping at the next numbered section.
- [ ] 2.2 Add unit tests for section-1 extraction with plain text, HTML-rich text, missing section 1, and mixed formatting edge cases.
- [ ] 2.3 Return a manual-review status with blank description when section 1 cannot be extracted, while still preserving Purchase Order in the generated row payload.

## 3. Billing invoice editor autofill flow
- [ ] 3.1 Implement client-side parsing for supported Job Number expressions (commas, slashes, de-duplication) and normalize canonical tokens for backend lookup.
- [ ] 3.2 Implement "Dirty State" detection for the line-item grid to track manual modifications.
- [ ] 3.3 Implement the Hybrid Trigger UI: a contextual "Refresh from Job Numbers" action that appears when the Job Number field no longer matches the current generated set.
- [ ] 3.4 Implement the confirmation dialog for overwriting dirty grid data.
- [ ] 3.5 Ensure Job Number edits alone never overwrite rows; only the explicit refresh action may trigger lookup and regeneration.
- [ ] 3.6 Wire the dialog to the lookup flow, replacing rows upon confirmation, highlighting unresolved job numbers in the UI, and flagging manual-review rows when section 1 is missing.
- [ ] 3.7 Populate generated invoice rows with Purchase Order and extracted section-1 Description, leaving Description blank for manual-review rows.

## 4. End-to-end validation
- [ ] 4.1 Add frontend tests covering parser expansion and the "Dirty State" confirmation flow.
- [ ] 4.2 Add frontend coverage proving Job Number changes expose the refresh action without auto-regenerating rows.
- [ ] 4.3 Add integration coverage for the full pipeline: `Input` $\rightarrow$ `Parse` $\rightarrow$ `Lookup` $\rightarrow$ `Extract` $\rightarrow$ `Grid`.
- [ ] 4.4 Verify that unresolved jobs and manual-review rows are clearly communicated to the user.
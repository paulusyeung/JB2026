## 1. Backend lookup contract

- [ ] 1.1 Add a batch lookup path for canonical job numbers that returns the billing editor fields needed for autofill, including purchaseOrder and productDetails inputs.
- [ ] 1.2 Extend or introduce API DTOs so the billing editor can distinguish resolved jobs, unresolved jobs, and jobs missing required section-1 description data.
- [ ] 1.3 Add API tests covering successful multi-job lookup, unresolved job numbers, and duplicate canonical job requests.

## 2. Description extraction logic

- [ ] 2.1 Implement plain-text normalization and section-1 extraction for Product Details, excluding the section header line and stopping at the next numbered section.
- [ ] 2.2 Add unit tests for section-1 extraction with plain text, HTML-rich text, missing section 1, and mixed formatting edge cases.

## 3. Billing invoice editor autofill flow

- [ ] 3.1 Implement client-side parsing for supported Job Number expressions, including comma-separated tokens, slash-expanded suffixes, de-duplication, and validation errors.
- [ ] 3.2 Wire BillingInvoiceEditorDialog to call the new lookup flow and replace the current auto-filled line-item set with one generated row per resolved job.
- [ ] 3.3 Populate generated invoice rows with Purchase Order and extracted section-1 Description while keeping the grid editable for manual adjustments.

## 4. End-to-end validation

- [ ] 4.1 Add frontend tests covering parser expansion, invalid syntax handling, and regeneration behavior when the Job Number value changes.
- [ ] 4.2 Add integration coverage for invoice item generation using representative examples such as `168824-1`, `168824-1, 168825-1`, `168824-1/2/3`, and `168824-1/2/3, 168825-1`.
- [ ] 4.3 Verify the final OpenSpec change is apply-ready and document any remaining UI decision points, such as whether autofill runs automatically or behind an explicit action.

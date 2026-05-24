## Why

BillingInvoiceEditorDialog currently accepts a free-form Job Number value but leaves invoice line items fully manual. Users already have structured job data in the same underlying job-order source family used by JobListView, so entering supported Job Number patterns should be enough to resolve matching jobs and prefill invoice items consistently.

This is needed now because the current manual copy-and-paste flow is slow, error-prone, and loses important formatting rules around multi-job inputs, purchase orders, and the section 1 subset of product details that billing expects.

## What Changes

- **Intelligent Parsing**: Add Job Number parsing rules in BillingInvoiceEditorDialog for empty input, comma-separated job references, slash-expanded suffix ranges, and combinations of both forms, using a canonical user-facing format of `orderNumber-jobSuffix`.
- **Batch Resolution**: Resolve each parsed Job Number through a billing-focused batch lookup backed by the same job-order source family used by JobListView, including numeric normalization of stored job suffixes such as `01` matching input suffix `1`.
- **Targeted Extraction**: Populate each generated line item with the job's Purchase Order and a normalized Description extracted specifically from section 1 of Product Details (excluding the section header line).
- **Hybrid Trigger Mechanism**:
    - The system should detect when the Job Number field differs from the current generated state of the line-item grid.
    - Autofill must run only from an explicit contextual "Refresh from Job Numbers" action/button that appears when a mismatch is detected.
- **Data Safety (Dirty Check)**: Implement a guard to prevent accidental data loss. If the line-item grid has been manually edited, the system must prompt the user for confirmation before regenerating and overwriting the current rows.
- **Granular Validation**: Define fallback behavior for unsupported formats and unresolved jobs. The UI should explicitly highlight which specific job numbers failed to resolve rather than providing a generic error.
- **Missing Section Handling**: If a job resolves but section 1 cannot be extracted, generate the row with its Purchase Order, leave Description blank, and flag it for manual review instead of inventing placeholder text.
- **Manual Override**: Preserve full manual editing capabilities after generation so users can review and adjust invoice items before saving.

## Capabilities

### New Capabilities
- `billing-invoice-editor-job-number-autofill`: Parse supported Job Number expressions and transform matching job-order data into invoice editor line items with safety guards and hybrid triggering.

### Modified Capabilities

## Impact

- **Frontend**: Affected code in BillingInvoiceEditorDialog, including new state management for "dirty" grid detection and the hybrid trigger UI.
- **Backend**: Affected backend billing and job-order APIs to add a billing-focused batch lookup that exposes Purchase Order and product detail inputs without widening the generic JobListView contract.
- **Logic**: New parsing, extraction, refresh-state, and validation logic spanning Vue UI state, TypeScript service contracts, and API/controller behavior.
- **Testing**: New automated coverage in web app tests and API tests for Job Number parsing, zero-padded suffix lookup normalization, dirty-state confirmation flows, and section-1 extraction edge cases.
## Why

BillingInvoiceEditorDialog currently accepts a free-form Job Number value but leaves invoice line items fully manual. Users already have structured job data in the same job-order source used by JobListView, so entering supported Job Number patterns should be enough to resolve matching jobs and prefill invoice items consistently.

This is needed now because the current manual copy-and-paste flow is slow, error-prone, and loses important formatting rules around multi-job inputs, purchase orders, and the section 1 subset of product details that billing expects.

## What Changes

- Add Job Number parsing rules in BillingInvoiceEditorDialog for empty input, comma-separated job references, slash-expanded suffix ranges, and combinations of both forms.
- Resolve each parsed Job Number against the job-order data source used by JobListView and build one invoice line item per resolved job.
- Populate each generated line item with the job's Purchase Order and a normalized Description extracted from section 1 of Product Details, excluding the section header line.
- Define validation and fallback behavior for unsupported formats, unresolved jobs, duplicate expansions, and jobs that do not expose the required billing fields.
- Preserve manual editing after generation so users can review and adjust invoice items before saving.

## Capabilities

### New Capabilities
- `billing-invoice-editor-job-number-autofill`: Parse supported Job Number expressions and transform matching job-order data into invoice editor line items.

### Modified Capabilities

## Impact

- Affected frontend code in BillingInvoiceEditorDialog and related billing service types.
- Likely affected backend billing and job-order APIs where Purchase Order and job lookup data may need to be exposed for the editor workflow.
- New parsing, extraction, and validation logic spanning Vue UI state, TypeScript service contracts, and API/controller behavior.
- New automated coverage in web app tests and API tests for Job Number parsing, product details extraction, and invoice item generation.

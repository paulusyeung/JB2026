## Context

BillingInvoiceEditorDialog currently exposes a free-form Job Number field and a fully manual line-items grid. The job-order experience already has structured job data available through the same API family used by JobListView, but the invoice editor does not parse Job Number expressions or reuse that source to prefill billing fields.

The feature crosses the Vue dialog, billing service contracts, and backend job-order or billing endpoints. The main constraint discovered during code inspection is that the frontend job-order list contract already includes productDetails but does not expose purchaseOrder, while the backend job entity and billing mapping logic already track a P.O. number. The stored data model also keeps order number and job number as separate fields, with legacy rows sometimes zero-padding the job suffix. The design therefore needs a deterministic parser plus a billing-focused lookup response that returns both purchaseOrder and productDetails for each resolved job while matching canonical user input against the existing separate-field storage model.

## Goals / Non-Goals

**Goals:**
- Accept the supported Job Number patterns in the invoice editor and expand them into distinct job references.
- Resolve parsed jobs through a billing-focused batch lookup backed by the same job-order source family used by JobListView.
- Generate one invoice line item per resolved job, using Purchase Order for the line P.O. value and section 1 of Product Details for Description.
- Require an explicit refresh action before replacing invoice rows generated from Job Numbers.
- Keep generated rows editable and surface clear validation when parsing or lookup is incomplete.
- Cover parser behavior, extraction rules, and lookup failures with automated tests.

**Non-Goals:**
- Automatic pricing, quantity, or unit-cost derivation for generated invoice items.
- Changing the existing manual line-item editing workflow outside the new autofill path.
- Supporting arbitrary Job Number shorthand beyond the explicitly approved comma and slash suffix formats.
- Reworking JobListView behavior unrelated to its existing data source contract.

## Decisions

### 1. Parse Job Number expressions on the client into canonical tokens before lookup
The dialog should normalize user input into an ordered, de-duplicated list of canonical job references before calling any API. Supported forms are:
- single job: `168824-1`
- comma-separated jobs: `168824-1, 168825-1`
- slash expansion on a shared prefix: `168824-1/2/3`
- mixed forms: `168824-1/2/3, 168825-1`

The canonical token is a user-facing composite string, but lookup matching should split it into `orderNumber` plus a numeric `jobSuffix` so that stored values such as `01` match input suffix `1`. Client-side parsing gives immediate validation, prevents ambiguous server behavior, and keeps the lookup request explicit. Unsupported tokens should fail fast with a user-facing validation message instead of partially guessing intent.

Alternative considered: parsing only on the backend. Rejected because it hides syntax errors until submit time and duplicates simple UI validation concerns that belong near the field.

### 2. Introduce a lookup contract that returns invoice-generation fields for multiple canonical job numbers
The billing editor needs more than the current create/update invoice DTOs. It should call a dedicated lookup endpoint or billing-focused job lookup service that accepts canonical job numbers and returns, per job:
- canonical job number
- stable job/order identifier
- purchaseOrder
- productDetails
- optional status flags for unresolved or manual-review records

This should be implemented as a dedicated billing endpoint or billing-focused job lookup service backed by the existing job-order repository data. The important design constraints are that the response remains batch-oriented so one field edit resolves all requested jobs in a single round trip, and that the generic JobListView contract does not need to grow a billing-specific purchaseOrder field.

Alternative considered: chaining one existing detail endpoint call per job. Rejected because it increases latency and does not solve the missing purchaseOrder contract.

### 3. Extract invoice descriptions from Product Details section 1 using plain-text normalization
Generated descriptions should come from section 1 only. The extraction pipeline should:
- normalize HTML or rich-text product details into plain text
- locate the first numbered section beginning with `1.`
- discard the first line of that section, such as `1.印刷內容：`
- keep subsequent non-empty lines until the next numbered section begins
- preserve meaningful line breaks and indentation already present in the section body

If section 1 is missing or empty after normalization, the lookup result should still return the resolved job with a manual-review flag. The generated invoice row should keep its Purchase Order, leave Description blank, and surface that row as needing manual review instead of inventing placeholder text.

Alternative considered: reusing the existing billing helper that concatenates orderTitle and productDetails. Rejected because it does not satisfy the requested section-specific extraction rule.

### 4. Hybrid Trigger and Data Safety
To balance automation with user control, the autofill process will follow a hybrid trigger model:
- **Detection**: The UI monitors the Job Number field. If the value changes and differs from the current grid state, a "Refresh from Job Numbers" button/icon appears.
- **Explicit Trigger**: Autofill runs only when the user clicks the contextual refresh action. Changing or blurring the Job Number field alone does not regenerate rows.
- **Dirty State Guard**: Before executing the refresh, the system checks if the line-item grid has been manually modified (is "dirty").
- **Confirmation**: If the grid is dirty, a confirmation dialog is shown: *"You have manual changes in your invoice items. Regenerating from Job Numbers will overwrite them. Proceed?"*
- **Execution**: Upon confirmation (or if the grid is clean), the current auto-generated set is replaced with the latest resolved items.

### 5. Granular Resolution Feedback
Instead of a binary success/failure, the lookup response and UI will handle partial success:
- **Resolved**: Item is added to the grid.
- **Unresolved**: The specific job number is flagged in the UI (e.g., red text or a warning icon) so the user knows exactly which reference failed.
- **Missing Data**: If a job is found but Section 1 is missing, the row is created with its Purchase Order, a blank description, and a "Manual Review Required" flag.

## Risks / Trade-offs

- [Frontend/backend contract gap for purchaseOrder] → Add a dedicated response shape and cover it with API tests before wiring the UI.
- [Product Details formatting varies between legacy records] → Normalize to plain text, detect numbered sections conservatively, and fail visibly when section 1 cannot be extracted.
- [Partial lookup success across multiple job numbers] → Return per-job resolution status so the UI can report unresolved jobs and avoid silently generating incomplete invoices.
- [Users may overwrite manual rows accidentally when regenerating] → Scope replacement to the autofill action, make the action explicit, and show which job numbers were resolved.

## Migration Plan

1. Add backend lookup support for canonical job-number batches and expose purchaseOrder plus productDetails-derived extraction inputs.
2. Add frontend parser and autofill action in BillingInvoiceEditorDialog behind the existing manual editor workflow.
3. Add tests for parser expansion, lookup mapping, and description extraction edge cases.
4. Roll out without data migration because the feature only affects create/edit behavior for draft invoice content.
5. Roll back by disabling the new autofill action and preserving the existing manual-entry path.

## Open Questions

- When some job numbers resolve and others fail, should the UI block all generation or allow partial generation with warnings?
- Does the backend already have a repository projection with purchaseOrder for job-list records, or is a new billing-specific projection the cleaner path?

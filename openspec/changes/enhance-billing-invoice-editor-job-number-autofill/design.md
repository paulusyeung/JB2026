## Context

BillingInvoiceEditorDialog currently exposes a free-form Job Number field and a fully manual line-items grid. The job-order experience already has structured job data available through the same API family used by JobListView, but the invoice editor does not parse Job Number expressions or reuse that source to prefill billing fields.

The feature crosses the Vue dialog, billing service contracts, and backend job-order or billing endpoints. The main constraint discovered during code inspection is that the frontend job-order list contract already includes productDetails but does not expose purchaseOrder, while the backend job entity and billing mapping logic already track a P.O. number. The design therefore needs a deterministic parser plus a lookup response that returns both purchaseOrder and productDetails for each resolved job.

## Goals / Non-Goals

**Goals:**
- Accept the supported Job Number patterns in the invoice editor and expand them into distinct job references.
- Resolve parsed jobs through the same job-order source family used by JobListView.
- Generate one invoice line item per resolved job, using Purchase Order for the line P.O. value and section 1 of Product Details for Description.
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

Client-side parsing gives immediate validation, prevents ambiguous server behavior, and keeps the lookup request explicit. Unsupported tokens should fail fast with a user-facing validation message instead of partially guessing intent.

Alternative considered: parsing only on the backend. Rejected because it hides syntax errors until submit time and duplicates simple UI validation concerns that belong near the field.

### 2. Introduce a lookup contract that returns invoice-generation fields for multiple canonical job numbers
The billing editor needs more than the current create/update invoice DTOs. It should call a dedicated lookup endpoint or billing-focused job lookup service that accepts canonical job numbers and returns, per job:
- canonical job number
- stable job/order identifier
- purchaseOrder
- productDetails
- optional status flags for missing or unusable records

This can be implemented either by extending the existing job-order API surface or by adding a billing endpoint that composes existing repository data. The important design constraint is that the response must be batch-oriented so one field edit resolves all requested jobs in a single round trip.

Alternative considered: chaining one existing detail endpoint call per job. Rejected because it increases latency and does not solve the missing purchaseOrder contract.

### 3. Extract invoice descriptions from Product Details section 1 using plain-text normalization
Generated descriptions should come from section 1 only. The extraction pipeline should:
- normalize HTML or rich-text product details into plain text
- locate the first numbered section beginning with `1.`
- discard the first line of that section, such as `1.印刷內容：`
- keep subsequent non-empty lines until the next numbered section begins
- preserve meaningful line breaks and indentation already present in the section body

If section 1 is missing or empty after normalization, the lookup result should mark that job as non-generatable so the UI can ask for manual review instead of injecting a malformed description.

Alternative considered: reusing the existing billing helper that concatenates orderTitle and productDetails. Rejected because it does not satisfy the requested section-specific extraction rule.

### 4. Replace generated line items as a single refresh action, then allow manual edits
When the user triggers autofill for a valid Job Number value, the dialog should replace the current auto-generated set with the latest resolved items in parsed order. Manual edits remain allowed after generation, and users can still add or remove rows afterward.

This avoids mixing stale auto-generated rows with new input expansions. If the field is empty, the dialog should not auto-generate anything and should leave the manual grid unchanged.

Alternative considered: incrementally appending generated rows. Rejected because repeated edits to the Job Number field would create duplicate or stale items that are hard to reason about.

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

- Should autofill run automatically on blur/change of the Job Number field, or only from an explicit action such as a button?
- When some job numbers resolve and others fail, should the UI block all generation or allow partial generation with warnings?
- Does the backend already have a repository projection with purchaseOrder for job-list records, or is a new billing-specific projection the cleaner path?

## 1. Frontend print-manager flow

- [x] 1.1 Create a shared job-order print-manager dialog component that captures layout, no-picture, no-product-details, and workflow-selection options for an existing order.
- [x] 1.2 Replace the JobListView print action with dialog launch and remove the job-order `window.print()` behavior from that path.
- [x] 1.3 Update JobOrderForm and all shared `print-order` consumers to launch the same print-manager flow instead of directly calling the fixed PDF blob endpoint.
- [x] 1.4 Add frontend tests for dialog opening, option submission, validation, and user-facing error handling.

## 2. Backend print contract and report composition

- [x] 2.1 Add a parameterized job-order print API contract and endpoint that returns `application/pdf` for a selected order plus print options.
- [x] 2.2 Implement a job-order print composition service that maps job data, workflows, product details, remarks, and attachments into a print document model.
- [x] 2.3 Implement QuestPDF job-order report rendering for the supported layout set, including suppression toggles and workflow filtering.
- [x] 2.4 Add structured logging and service-side error handling for failed job-order print requests.

## 3. Parity and rollout validation

- [x] 3.1 Add backend tests covering successful PDF responses, option propagation, and workflow-filter behavior.
- [x] 3.2 Add parity-focused PDF content tests for legacy header fields, optional-section omission, and multilingual text rendering.
- [ ] 3.3 Validate the generated default job-order PDF against representative legacy samples from PrintManager and capture any unsupported legacy layouts or options.
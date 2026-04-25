## 1. API and report pipeline

- [x] 1.1 Add a stock print API endpoint that returns `application/pdf` for a product ID.
- [x] 1.2 Implement a report data composer that maps product + movement data into print sections and deterministic row ordering/numbering.
- [x] 1.3 Introduce a PDF renderer abstraction and concrete implementation with multilingual (CJK-capable) font configuration.
- [x] 1.4 Add structured error handling/logging for print failures including product ID and root-cause details.

## 2. Frontend print integration

- [x] 2.1 Add stock service client method to request product print PDF as Blob/binary response.
- [x] 2.2 Replace the gated Print action in Product Record dialog with real print invocation in edit mode.
- [x] 2.3 Implement browser open/download fallback behavior and localized user-facing error messaging for failed print requests.

## 3. Parity and regression tests

- [x] 3.1 Add backend tests validating PDF response type and successful print endpoint behavior.
- [x] 3.2 Add report-content parity tests for required sections, column presence, and deterministic row ordering/numbering.
- [x] 3.3 Add multilingual rendering verification tests (including CJK text fixtures) to prevent missing-glyph regressions.
- [x] 3.4 Add frontend test coverage for print button behavior, request dispatch, and error-state messaging.

## 4. Rollout readiness

- [x] 4.1 Document print endpoint contract and operational notes (font dependency/configuration) in project docs.
- [ ] 4.2 Validate output against representative legacy sample PDFs and capture sign-off checklist items.

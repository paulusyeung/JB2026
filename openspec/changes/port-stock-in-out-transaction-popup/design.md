# Design - port-stock-in-out-transaction-popup

## Context

Legacy JB2015 exposes Stock In/Out as a compact popup (`StockInOut.cs`) with Save & Close-first interaction, fields for stock number/date/reference/qty, and strict validation before persistence. In JB2026, `StockView` and `ProductRecordDialog` currently show Stock In/Out actions but no transaction-entry implementation, leaving inventory movement registration outside the modern flow.

The design must preserve behavioral parity while fitting existing Vue 3 + Vuetify patterns and current stock service abstractions.

## Goals / Non-Goals

**Goals:**
- Introduce a reusable Stock In/Out dialog component in ClientApp.
- Support two launch points: `StockView` selected-row action and `ProductRecordDialog` action.
- Preserve legacy validation semantics for stock number existence and numeric qty input.
- Persist transactions through API and refresh stock list/movement history in the originating views.
- Keep Save & Close as primary action behavior.

**Non-Goals:**
- Porting legacy toolbar internals/icon resources pixel-for-pixel.
- Adding bulk stock adjustments in this slice.
- Reworking inventory accounting rules beyond current legacy parity behavior.
- Implementing historical edit/delete for existing stock in/out entries.

## Decisions

1. Dialog component and invocation contract
- Decision: Build a dedicated `StockInOutDialog` component with explicit props (`modelValue`, `productId`, `stockNumber`) and emits (`saved`, `close`).
- Rationale: Keeps launch behavior consistent across both entry points and avoids duplicating form state/validation logic.
- Alternatives considered:
  - Inline form in `ProductRecordDialog`: rejected because `StockView` also needs the same flow.
  - Route-level page instead of dialog: rejected due to mismatch with legacy mental model and extra navigation overhead.

2. Context-first stock number handling
- Decision: Pass selected product context from caller and prefill stock number in read-only mode by default.
- Rationale: In both requested entry points user intent starts from a known product; minimizing editable keys reduces wrong-product transactions.
- Alternatives considered:
  - Fully editable stock number field: accepted only as fallback mode when no product context exists (future-safe), but not primary path.

3. API orchestration and consistency refresh
- Decision: Add/extend stock service method for transaction creation and trigger caller refresh on successful save.
- Rationale: Dialog should remain focused on entry/validation while parent views own list/movement reload.
- Alternatives considered:
  - Let dialog mutate shared store directly: rejected to avoid hidden side effects and coupling to store implementation details.

4. Validation parity policy
- Decision: Keep parity-level validation rules in UI and enforce again server-side.
- Rationale: Legacy flow validates client-side for immediacy; server validation remains mandatory for integrity.
- Alternatives considered:
  - Server-only validation: rejected due to poorer UX and avoidable round-trips.

5. Save action semantics
- Decision: Expose Save and Save & Close buttons, with Save & Close styled/positioned as primary.
- Rationale: Legacy form intentionally de-emphasized Save-only and teams are accustomed to quick single-entry close flow.
- Alternatives considered:
  - Save-only action: rejected due to parity and throughput impact for clerical usage.

## Risks / Trade-offs

- [Risk] Divergence between UI and API validation messages. -> Mitigation: Define shared error contracts and map validation codes to i18n keys.
- [Risk] Race conditions if parent refresh is skipped after dialog save. -> Mitigation: Make `saved` emit mandatory and wire explicit reload handlers in both callers.
- [Risk] Negative quantity handling could drift from legacy integer rules. -> Mitigation: Add unit tests for `+/-` parsing and reject decimal quantities.
- [Risk] Two launch points may drift in behavior over time. -> Mitigation: Use one dialog and one service path; test both entry triggers in component tests.

## Migration Plan

1. Add dialog component and stock transaction service contract behind feature-complete UI behavior.
2. Wire launch from `StockView` (only enabled when exactly one row is selected) and from `ProductRecordDialog` action button.
3. Implement API endpoint integration for create transaction + balance update parity.
4. Add/extend tests for client interaction and API correctness/parity.
5. Release behind existing stock module route with no schema-breaking API changes.
6. Rollback plan: disable new launch wiring and keep previous gated action message if regression appears.

## Open Questions

- Should `StockView` allow Stock In/Out when multiple rows are selected (legacy suggests single-record context only)?
- Do we require role/permission gating for stock movement entry beyond existing stock page access?
- Should reference field length/character constraints be inherited from legacy DB schema now or deferred to backend validation response?

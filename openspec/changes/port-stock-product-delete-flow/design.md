# Design - port-stock-product-delete-flow

## Context

JB2015 supports product delete from list and record flows, but delete in JB2026 ClientApp stock surfaces is still unimplemented. Legacy behavior is not a single hard delete: it first retires the product, then only hard-deletes already-retired products while cleaning dependent stock movements, attachments, and image files. The modern implementation must preserve this lifecycle across both `StockView` and `ProductRecordDialog` while keeping API integrity and parity testability.

## Goals / Non-Goals

**Goals:**
- Implement usable delete actions in both stock entry points (`StockView`, `ProductRecordDialog`).
- Preserve legacy two-step deletion lifecycle semantics: retire first, hard-delete second.
- Preserve hard-delete cleanup semantics for stock in/out rows, product attachments, and image files.
- Support single and checkbox-based list deletion with clear confirmation and result feedback.
- Refresh UI state after delete so list and dialog content remain consistent.
- Add parity/correctness test coverage for lifecycle transitions and cleanup.

**Non-Goals:**
- Rebuilding legacy toolbar visual assets or WinForms UX details.
- Introducing undo/restore beyond existing retire lifecycle.
- Changing stock accounting/business rules outside current delete parity.
- Adding multi-step archival workflows outside retire/hard-delete behavior.

## Decisions

1. Shared delete orchestration path for both UI entry points
- Decision: Route both `StockView` and `ProductRecordDialog` delete actions through the same stock service delete method and common response contract.
- Rationale: Prevents divergence between list and record behaviors and reduces duplicate error/confirmation handling.
- Alternatives considered:
  - Separate per-view delete logic: rejected due to parity drift risk.
  - Dialog-local API call only: rejected because list batch delete also needs the same semantics.

2. Explicit lifecycle-aware API result contract
- Decision: API delete response SHALL include lifecycle outcome (`retired` vs `hardDeleted`) and target product id.
- Rationale: UI needs deterministic messaging and refresh behavior that matches legacy expectations.
- Alternatives considered:
  - Boolean success only: rejected because it cannot distinguish first-pass retire from hard-delete.

3. Transactional hard-delete cleanup in backend domain layer
- Decision: Implement hard-delete cleanup (stock movements, attachment rows, file deletion orchestration, product delete) in one domain operation guarded by server-side checks.
- Rationale: Legacy behavior relies on cascading cleanup; centralizing this logic avoids partial deletes from client interruption.
- Alternatives considered:
  - Client-triggered multiple API calls: rejected because it risks partial state and file/db mismatch.

4. Batch delete policy for checkbox mode
- Decision: In `StockView`, checkbox mode SHALL process selected products one-by-one and report aggregate outcomes (successes/failures) while continuing after non-fatal failures.
- Rationale: Aligns with legacy list workflow intent and avoids all-or-nothing UX for mixed data states.
- Alternatives considered:
  - Entire batch rollback on first failure: rejected due to poor operator throughput.

5. Confirmation and feedback parity policy
- Decision: Keep explicit user confirmation before delete operations and map lifecycle outcomes to specific i18n messages.
- Rationale: Legacy flow enforces confirmation and users need clarity about retire versus hard-delete effects.
- Alternatives considered:
  - Silent delete with snackbar only: rejected due to destructive action risk.

## Risks / Trade-offs

- [Risk] File cleanup failures during hard delete can leave orphan metadata or files. -> Mitigation: perform cleanup within guarded backend flow and log actionable failure details; fail operation when integrity cannot be guaranteed.
- [Risk] Batch deletion may become slow for large selections. -> Mitigation: cap UI batch size for this slice and show progress summary.
- [Risk] Users may misinterpret first-pass retire as permanent delete. -> Mitigation: show lifecycle-specific success text and row-state refresh.
- [Risk] Existing tests may not cover legacy two-pass semantics. -> Mitigation: add explicit parity tests for retire-first, hard-delete-second behavior.

## Migration Plan

1. Add backend delete lifecycle support and response contract for retire/hard-delete outcomes.
2. Implement shared stock service delete API integration in ClientApp.
3. Wire delete actions in `StockView` and `ProductRecordDialog` to shared flow.
4. Add lifecycle-specific confirmations/messages and list/dialog refresh handlers.
5. Add frontend tests and API parity/correctness tests for single and batch flows.
6. Rollback plan: disable new delete action wiring and restore current unavailable-action behavior if regressions appear.

## Open Questions

- Should hard delete require an additional stronger confirmation compared to first-pass retire?
- Should batch delete responses return per-item lifecycle outcome and reason codes for UI summary messages?
- Are there permission distinctions between retire and hard-delete operations in current JB2026 role policy?

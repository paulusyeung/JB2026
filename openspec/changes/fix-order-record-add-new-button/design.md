"# Design: Fix Order Record ADD NEW Button

## Context

Currently, `OrderRecordDialog` has an "ADD NEW" button that calls `resetDraft()`, which only resets the local form state. The user sees no change because:
1. The dialog remains in edit mode for the current order
2. No new job is created
3. No navigation or feedback occurs

The existing pattern in the codebase shows that `OrderRecordDialog` emits events like `saved`, `deleted`, `open-order`, and `open-job-form` to communicate with the parent `OrderListView`. The `JobOrderForm` component handles actual job creation when opened with `job=null`.

## Goals / Non-Goals

**Goals:**
- Make "ADD NEW" button actually create a new job through `JobOrderForm`
- Follow existing event-based communication pattern between child and parent components
- Preserve order context (orderNumber, customerName) when creating the new job
- Refresh data after save so the new job appears in the related orders list

**Non-Goals:**
- Not modifying backend APIs - existing `createJobOrder` service is sufficient
- Not changing `JobOrderForm` behavior - it already supports create mode
- Not adding new validation or business logic

## Decisions

### Decision 1: Emit a new event from OrderRecordDialog
**Why**: Follows the existing pattern where `OrderRecordDialog` emits events (`saved`, `deleted`, `open-job-form`) to let the parent control navigation and state.

**Alternative considered**: Direct API call from `OrderRecordDialog`
- Rejected because it duplicates logic that already exists in `JobOrderForm` and breaks the separation of concerns.

### Decision 2: Parent closes OrderRecordDialog, opens JobOrderForm with job=null
**Why**: The existing `handleJobSaved` in `OrderListView` already handles refresh + close. We just need to trigger it via the create path.

**Flow**:
1. User clicks "ADD NEW" in OrderRecordDialog
2. OrderRecordDialog emits `add-new-job` with the parent order's context (orderId, orderNumber, customerName)
3. Parent handler in OrderListView:
   - Closes OrderRecordDialog (`formOpen = false`)
   - Opens JobOrderForm with minimal job data to set defaults (`jobFormJob.value = { orderId: null, ... }`)
4. After save in JobOrderForm:
   - `handleJobSaved` refreshes the list
   - Optionally reopens OrderRecordDialog for the same order

### Decision 3: Pass parent order context to pre-fill create form
**Why**: When creating a new job within an existing order, the user expects fields like `orderNumber`, `customerName`, and `orderedBy` to be inherited from the parent order. This is already handled in `OrderRecordDialog.buildCreateDraft()` but we need similar defaults in `JobOrderForm`.

**Approach**: Emit the parent order's key fields so JobOrderForm can pre-populate them. The existing `buildCreateDraft` in OrderRecordDialog shows the pattern:
```typescript
customerName: props.order?.customerName ?? '',
orderedBy: session.profile?.displayName ?? props.order?.orderedBy ?? '',
```

## Risks / Trade-offs

[Risk] Other views using OrderRecordDialog might not handle the new event → **Mitigation**: Events are optional to handle. Add handler only in `OrderListView` initially, other views can add later if needed.

[Trade-off] Two-step flow (close dialog → open form) vs inline creation → **Decision**: Two-step is consistent with existing UX where JobOrderForm is always shown in its own dialog. No need to complicate by embedding it inline.

## Migration Plan

No migration needed - this is a pure frontend enhancement with no data model or API changes.

## Open Questions

None - the implementation path is clear based on existing patterns.
"
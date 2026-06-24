## Context

The Job Order system has a disconnect between frontend workflow attribute handling and backend persistence. The frontend component `JobOrderForm.vue` collects dynamic workflow attributes but these values are lost upon save because:
1. Frontend service layer strips them from API payloads
2. Backend DTOs lack properties to receive them
3. Repository does not persist them to the legacy JobWorkflow table structure
4. API responses don't include workflowAttributes for edit mode hydration

The legacy application (`JobRecord.cs`) handles this by calling `SaveWorkflow()` which upserts `JobWorkflow` rows keyed by `(OrderId, WorkflowId)` — loading an existing row by that key, updating it, or inserting a new one. `WorkIndex` is positional and re-indexed sequentially on every load.

## Goals / Non-Goals

**Goals:**
- Enable end-to-end persistence of workflow attribute values from UI through API to database
- Ensure workflow attributes load correctly when editing existing job orders
- Maintain compatibility with legacy JobWorkflow table structure
- Follow existing .NET 8 patterns and conventions

**Non-Goals:**
- Changing the underlying database schema for JobWorkflow table
- Modifying how workflow definitions (available options) are managed
- Implementing new validation rules for workflow attribute values

## Decisions

1. **Use Dictionary<string, string> for DTO transport**: Workflow attributes will be transported as a simple key-value dictionary in request/response DTOs. This matches the frontend `Record<string, string>` pattern and is easy to serialize via JSON.

2. **Store value in JobWorkflow.WorkTitle, upsert by (OrderId, WorkflowId)**: The repository mirrors the legacy `SaveWorkflow()` pattern exactly:
   - Upsert key: `(OrderId, WorkflowId)` — look up existing row; if found, update it; if not, create a new `JobWorkflow` row.
   - `WorkIndex` = the attribute's `WorkIndex` from `Z_OrderTypeWorkflow` definition.
   - `WorkTitle` = the user's selected value (e.g. `"Glossy"`) — same column the legacy uses for step labels.
   - `WorkStatus` = `null` (discriminator: attribute rows vs. real workflow steps).
   - `WorkInstruction` = `null`, `WorkNotes` = `null`.
   - `WorkflowId` = resolved from `Z_OrderTypeWorkflow` by matching `(OrderType, WorkflowName)` against the attribute's key.

3. **StyleTitles picks up attribute values as-is**: The existing `StyleTitles` query reads `WorkTitle` unconditionally — this is the same behavior as the legacy app. No filter is added because the legacy doesn't filter either.

4. **Include workflowAttributes in JobDetailResponse only (not in list responses)**: Edit-mode hydration reads from `JobDetailResponse`. The list view (`JobOrderResponse`) does not include attributes to avoid extra queries against `vwOrderDetailList` which has no workflow data.

5. **Preserve existing workflow definitions endpoint**: The existing `getOrderTypeWorkflowAttributes` endpoint returns available field definitions. This change only addresses persisting user-selected VALUES, not modifying the definition retrieval mechanism.

6. **Frontend buildDraft maps from JobDetail.workflowAttributes**: On edit, `workflowAttributeValues` local ref is populated from the response dictionary, keyed by workflow name. On create, it starts empty.

## Risks / Trade-offs

[Risk] If WorkflowId lookup fails (no matching Z_OrderTypeWorkflow for a given attribute name), the upsert key is unknown → Mitigation: Build a lookup dictionary `(OrderType, WorkflowName) → WorkflowId` before iterating attributes. Skip entries with no match and log warning.

[Risk] Existing job orders without workflow data may return null references → Mitigation: Use nullable dictionary types (`Dictionary<string, string>?`) and add null checks in frontend buildDraft

[Risk] Race conditions if multiple users edit same job order simultaneously → Mitigation: This is an existing risk in the current system; no additional locking mechanism introduced in this change

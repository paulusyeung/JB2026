## Context

`JobOrderForm.vue` contains a `legacy-attribute-grid` with three hardcoded `legacy-attribute-row` divs backed by `legacyPrintingPaper`, `legacyFinishingOutput`, and `legacyPackagingRequirement` refs. All three share the same `legacyAttributeOptions` computed (derived from payment terms). The indicator colors (blue, green, red) are also hardcoded.

The correct behavior depends on `Z_OrderTypeWorkflow` / `Z_Workflow` data: for a given order type, the workflows determine both the count of rows and the dropdown options (from `WorkTitle`, delimited by `;`). The existing admin API (`GET /api/v2/admin/order-type/workflows`) returns available/selected workflow IDs but does not include `WorkTitle` or preserve ordering by `WorkIndex`, so a new consumer-facing endpoint is needed.

The attribute values must persist — they are part of the job order data but the current schema stores them implicitly via the frontend refs only (not in `JobOrderFormData` or `JobOrderRecord`).

## Goals / Non-Goals

**Goals:**
- Dynamically render attribute rows on the `legacy-attribute-grid` based on the current `OrderType`.
- Each row displays the `WorkflowName` as label and a dropdown populated from `WorkTitle` (split by `;`).
- Fetch workflow attributes from a new backend endpoint when order type changes.
- Persist selected attribute values so they survive save/reload.
- Assign indicator colors algorithmically by row index.
- Remove all static attribute refs and the shared `legacyAttributeOptions`.

**Non-Goals:**
- Not touching the admin workflow configuration UI — that already exists separately.
- Not changing the existing `Z_OrderTypeWorkflow` / `Z_Workflow` database schema.
- Not internationalizing workflow names (they come from DB in Chinese).

## Decisions

1. **New backend endpoint: `GET /api/v2/order-types/{orderType}/workflow-attributes`**
   - Returns `{ workflowAttributes: OrderTypeWorkflowAttribute[] }` where each item has `workIndex`, `workflowName`, `workTitle` (raw semicolon string), and `options` (parsed array).
   - Rationale: A dedicated endpoint is cleaner than overloading the admin endpoint. It directly serves the consumer (JobOrderForm) without exposing internal join logic to the client.
   - Alternative considered: Reusing `GET /api/v2/admin/order-type/workflows` and adding fields. Rejected because that endpoint is admin-scoped and does not preserve `WorkIndex` ordering or deliver `WorkTitle`.

2. **Client-side parsing of `WorkTitle`**
   - Split `WorkTitle` by `;` on the frontend to build the dropdown items array.
   - Rationale: The semicolon delimiter is a simple format; parsing on the client avoids backend changes for what is essentially a presentation concern. The raw string is also stored for round-trip fidelity.

3. **Persist attribute values as a `Record<string, string>` on `JobOrderFormData` / `JobOrderRecord`**
   - Keyed by `WorkflowName` (e.g., `{ "印刷用紙": "成昌", "鋅版輸出": "nuStar", "包裝要求": "----" }`).
   - Rationale: A key-value map decouples the frontend rendering from any fixed schema. The current backend record likely stores these as separate columns (or not at all) — a map on the form DTO is the simplest integration without changing the database. For now, values live only in the form DTO during the session; backend persistence can be tackled as a follow-up if needed.
   - Alternative considered: Adding individual columns to the database for each workflow attribute. Rejected because workflows are dynamic per order type — columns would need to change whenever workflows change.

4. **Indicator color by row index**
   - Cycle through a predefined palette (e.g., blue, green, red, orange, purple, teal) based on `workIndex % palette.length`.
   - Rationale: Simple deterministic assignment without needing to map names to colors. The current three colors map directly to the first three palette entries.

5. **Watch `draft.orderType` to refetch attributes**
   - Use `watch(() => draft.value.orderType, ...)` to trigger a fetch when the order type changes.
   - Rationale: Existing pattern in the component (e.g., watching `props.job`). Avoids manual event handling.

## Risks / Trade-offs

- **Risk: New API endpoint adds surface area** → Mitigation: Keep the endpoint read-only and scope it to the job orders area. Add authorization consistent with other job-order endpoints.
- **Risk: Workflow attribute values not persisted to DB** → Mitigation: Document this as a known limitation. The form DTO carries values in-memory during the session. If backend persistence is required later, the `Record<string, string>` map can be serialized to the existing `MetadataXml` column.
- **Risk: `WorkTitle` format changes** → Mitigation: The semicolon-delimited format is stable (legacy system). Frontend parsing is trivially changeable. Include a defensive `.filter(Boolean)` to skip empty segments.

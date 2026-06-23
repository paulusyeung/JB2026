## Why

The `legacy-attribute-grid` in `JobOrderForm.vue` currently renders three static attribute rows (printing paper, finishing output, packaging requirement) regardless of the order type. This is incorrect — the number and content of attribute rows depends on the order type and its associated workflows. The grid must dynamically render rows based on the `OrderType` selected, fetching workflow configurations from the `Z_OrderTypeWorkflow` / `Z_Workflow` tables.

## What Changes

- Replace the three static `<div class="legacy-attribute-row">` elements with a `v-for` loop over a computed list of attribute definitions fetched per order type.
- Add a new backend API endpoint (or extend an existing one) that returns workflows for a given `OrderType`, including `WorkIndex`, `WorkflowName`, and `WorkTitle` (semicolon-delimited dropdown options).
- Add a new frontend service call to fetch the order-type-specific workflow attributes when the order type changes.
- Store workflow attribute selections on the `JobOrderFormData` / `JobOrderRecord` so they are saved and loaded with the order.
- Remove the now-unused `legacyPrintingPaper`, `legacyFinishingOutput`, `legacyPackagingRequirement` refs and `legacyAttributeOptions` computed.
- Replace the hardcoded indicator colors (blue/green/red) with a deterministic color assignment based on row index.

## Technical Feedback & Implementation Strategy

### 1. Backend Query Strategy: EF Core vs. Stored Procedures
*   **Recommendation:** Use **EF Core LINQ** for this read-only configuration query rather than creating a new Stored Procedure.
*   **Reasoning:** The data volume is small (configuration metadata). Using LINQ allows for easier maintenance of the "Semicolon-to-Array" parsing logic in C# code. It keeps the Stored Procedure layer reserved for complex writes or heavy transactional logic, while leveraging EF's existing mappings for `Z_OrderTypeWorkflow` and `Z_Workflow`.

### 2. Data Parsing Robustness
*   **Challenge:** Database columns store options as raw semicolon-delimited strings (e.g., `"Small;Medium;Large"`).
*   **Requirement:** The backend DTO projection must use robust parsing:
    *   Use `StringSplitOptions.RemoveEmptyEntries`.
    *   Apply `.Trim()` to each item to handle trailing/leading spaces.
    *   Handle nulls gracefully: Return an empty array `[]` if no options are defined, rather than `null`, to prevent frontend crashes in `v-select`.

### 3. Frontend State Management & UX
*   **Watch Order Type Changes:** Implement a `watch` on the `orderType` property of the job order draft. When it changes, the logic must:
    1.  Reset the dynamic attributes map (to prevent stale data from the previous order type).
    2.  Fetch new attributes.
    3.  Update the UI.
*   **Loading State:** Display a skeleton loader or disable grid rows during the fetch to prevent "Flash of Unstyled Content" (FOUC) where stale data might briefly appear before new data arrives.

### 4. Future-Proofing: Attribute Types
*   **Recommendation:** Structure the DTO and Vue component to support different input types beyond just dropdowns.
*   **Implementation:** Use a generic `<component :is="inputComponentType" ... />` pattern in the `v-for` loop, driven by an `attributeType` enum (e.g., `Dropdown`, `TextInput`) in the DTO. This allows future expansion to date pickers or number inputs without major refactoring of the grid template.

### 5. Error Handling: Missing Configurations
*   **Scenario:** An Order Type may exist but have no workflows assigned.
*   **Requirement:** The API should return an empty list `[]` in this case, not a 404 or 500 error.
*   **UI Gracefulness:** The frontend should display a helpful message (e.g., "No attributes configured for this Order Type") rather than an empty/broken grid.

## Capabilities

### New Capabilities
- `order-type-workflow-attributes`: Fetch and display dynamic attribute rows based on order type and linked workflows. Each row shows a `WorkflowName` label and a dropdown populated from the semicolon-delimited `WorkTitle` values.

### Modified Capabilities
- *(none — no existing specs govern the legacy attribute grid)*

## Impact

- **JobOrderForm.vue** — template rewritten to render rows dynamically; three static refs removed; new reactive state for workflow attributes.
- **Backend** — new API endpoint `GET /api/v2/order-types/{orderType}/workflow-attributes` (or similar) returning `WorkIndex`, `WorkflowName`, `WorkTitle`.
- **TypeScript types** — new `OrderTypeWorkflowAttribute` interface; updated `JobOrderFormData` / `JobOrderRecord` to include a map of attribute values.
- **Backend service** — new query against `Z_OrderTypeWorkflow` / `Z_Workflow` using EF Core LINQ for flexibility.
- **i18n** — translations for dynamically-rendered workflow labels are handled on the backend (Chinese names from DB), no frontend i18n needed for attribute row labels.


## Why

Currently, `workflowAttributes` (dynamic job-specific fields like "Printing Paper") entered in the frontend `JobOrderForm.vue` are lost upon saving. This is caused by a full-stack disconnect: the frontend service layer strips them from the API payload, the backend DTOs lack properties to receive them, and the repository does not persist them to the legacy `JobWorkflow` table structure.

## What Changes

- **Frontend (`jobs.ts` & `api.ts`)**: Update TypeScript interfaces and service mappings to include `workflowAttributes` in request payloads (Create/Update) and the `JobDetail` response model. List responses (`JobOrderRecord`) excluded.
- **Backend DTOs**: Add `Dictionary<string, string>` properties for workflow attributes to `CreateJobOrderRequest`, `UpdateJobOrderRequest`, and `JobDetailResponse`.
- **Backend Repository**: Implement logic in `EfJobManagementRepository` to persist the flat dictionary into the legacy `JobWorkflow` table, matching the legacy `SaveWorkflow()` method in `JobRecord.cs` exactly: upsert by `(OrderId, WorkflowId)`, store value in `WorkTitle`, set `WorkStatus = null`, and resolve `WorkflowId` from `Z_OrderTypeWorkflow` by attribute name.
- **Frontend Initialization**: Fix `buildDraft` in `JobOrderForm.vue` to hydrate existing workflow attributes from `JobDetail.workflowAttributes` when loading an existing job order.

## Capabilities

### New Capabilities
- `workflow-attribute-persistence`: End-to-end persistence of dynamic workflow attribute values from the UI through the API to the database, ensuring they are saved and retrieved correctly for both new and existing job orders.

### Modified Capabilities
- None

## Impact

- **Frontend**: `ClientApp/src/types/api.ts`, `ClientApp/src/services/jobs.ts`, `ClientApp/src/components/forms/JobOrderForm.vue`.
- **Backend**: `JB2026.Api/Models/CreateJobOrderRequest.cs`, `UpdateJobOrderRequest.cs`, `JobDetailResponse.cs`, `JB2026.Api/Services/EfJobManagementRepository.cs`, `JB2026.Api/Services/InMemoryJobManagementRepository.cs`.
- **Database**: Operations against the `JobWorkflow` (or equivalent) table will now occur during standard Job Order Create/Update operations via the new API.
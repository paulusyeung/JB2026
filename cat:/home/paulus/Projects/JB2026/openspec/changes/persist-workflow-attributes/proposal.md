## Why

Currently, `workflowAttributes` (dynamic job-specific fields like "Printing Paper") entered in the frontend `JobOrderForm.vue` are lost upon saving. This is caused by a full-stack disconnect: the frontend service layer strips them from the API payload, the backend DTOs lack properties to receive them, and the repository does not persist them to the legacy `JobWorkflow` table structure.

## What Changes

- **Frontend (`jobs.ts` & `api.ts`)**: Update TypeScript interfaces and service mappings to include `workflowAttributes` in both request payloads (Create/Update) and response models (`JobDetail`, `JobOrderRecord`).
- **Backend DTOs**: Add `Dictionary<string, string>` properties for workflow attributes to `CreateJobOrderRequest`, `UpdateJobOrderRequest`, and response models.
- **Backend Repository**: Implement logic in `EfJobManagementRepository` to map the flat dictionary of workflow attributes back into the legacy `JobWorkflow` table structure (matching the behavior of the legacy `SaveWorkflow` method in `JobRecord.cs`).
- **Frontend Initialization**: Fix `buildDraft` in `JobOrderForm.vue` to correctly hydrate existing workflow attributes when loading an existing job order.

## Capabilities

### New Capabilities
- `workflow-attribute-persistence`: End-to-end persistence of dynamic workflow attribute values from the UI through the API to the database, ensuring they are saved and retrieved correctly for both new and existing job orders.

### Modified Capabilities
- None

## Impact

- **Frontend**: `ClientApp/src/types/api.ts`, `ClientApp/src/services/jobs.ts`, `ClientApp/src/components/JobOrderForm.vue`.
- **Backend**: `JB2026.Api/Models/CreateJobOrderRequest.cs`, `UpdateJobOrderRequest.cs`, `JobOrderResponse.cs`, `JB2026.Core/Repositories/EfJobManagementRepository.cs`.
- **Database**: Operations against the `JobWorkflow` (or equivalent) table will now occur during standard Job Order Create/Update operations via the new API.
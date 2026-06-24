## Tasks

### Backend - DTO Updates
- [x] Add `public Dictionary<string, string>? WorkflowAttributes { get; init; }` to `CreateJobOrderRequest.cs`
- [x] Add `public Dictionary<string, string>? WorkflowAttributes { get; init; }` to `UpdateJobOrderRequest.cs`
- [x] Add `public Dictionary<string, string>? WorkflowAttributes { get; init; }` to `JobDetailResponse.cs`
- [x] Do NOT add to `JobOrderResponse.cs` — list responses excluded

### Backend - Repository Implementation (EfJobManagementRepository)
- [x] Build a lookup dictionary `(OrderType, WorkflowName) → (WorkflowId, WorkIndex)` from `Z_OrderTypeWorkflow` so attribute keys can be mapped to their FK and position
- [x] In `CreateJobOrder`: after saving the JobOrder entity, iterate `WorkflowAttributes` dictionary, upsert `JobWorkflow` rows by `(OrderId, WorkflowId)` — same upsert pattern as legacy `SaveWorkflow()`
- [x] In `UpdateJobOrder`: same upsert logic; also remove orphaned `JobWorkflow` rows where `WorkStatus == null` that no longer appear in the dictionary
- [x] In `MapDetail()`: reconstruct `workflowAttributes` dictionary from `JobWorkflow` rows where `WorkStatus == null`, keyed by `Z_Workflow.WorkflowName` (requires joining through `Workflow` navigation property)
- [x] `StyleTitles` needs no change — it reads `WorkTitle` unconditionally, matching legacy behavior

### Backend - InMemory Repository
- [x] Add `Dictionary<string, string>? WorkflowAttributes` to the internal `JobRecord` record
- [x] Update `CreateJobOrder` to store the dictionary
- [x] Update `UpdateJobOrder` to merge the dictionary
- [x] Update `MapDetail` to return it in response

### Backend - Controller Updates
- [x] No controller changes needed — the `JobsController.Create`/`Update` already accept `CreateJobOrderRequest`/`UpdateJobOrderRequest` and pass them to the repository. Adding properties to the DTOs is sufficient.

### Frontend - Type Definitions
- [x] Add `workflowAttributes?: Record<string, string>` to `JobDetail` interface in `api.ts`
- [x] Do NOT add to `JobOrderRecord` — list responses won't include it
- [x] Update internal request interfaces in `jobs.ts` (`CreateJobRequest`, `UpdateJobRequest`) to include `workflowAttributes` property

### Frontend - Service Layer
- [x] Modify `saveJob` function in `jobs.ts` to include `workflowAttributes` in POST/PUT payloads
- [x] Ensure mapping from `JobOrderFormData` preserves `workflowAttributeValues` through to API request

### Frontend - Component Fixes
- [x] Update `buildDraft` in `JobOrderForm.vue` for existing jobs: map `job.workflowAttributes` (from `JobDetail`) into local `workflowAttributeValues` ref
- [x] Add null/undefined checks for `workflowAttributes` when loading existing jobs
- [ ] Test edit flow: open saved job → verify values load → modify → save → verify persistence

### Testing & Verification
- [ ] Create new job order with workflow attributes → verify `JobWorkflow` table contains correct rows (WorkTitle = value, WorkStatus = null, WorkflowId resolved)
- [ ] Edit existing job order → verify workflow attributes load correctly via `JobDetail`
- [ ] Update workflow attributes on existing job → verify rows are upserted, old rows removed
- [ ] Test job order without workflow attributes → verify no errors (backward compatibility)
- [ ] Verify parity between `EfJobManagementRepository` and `InMemoryJobManagementRepository`

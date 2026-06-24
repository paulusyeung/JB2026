## ADDED Requirements

### Requirement: Frontend includes workflow attributes in save requests
The frontend job service SHALL include workflowAttributes from JobOrderFormData when constructing CreateJobRequest and UpdateJobRequest payloads sent to the backend API.

#### Scenario: Create job with workflow attributes included
- **WHEN** user submits a new job order with workflow attribute values filled in
- **THEN** the HTTP POST request body contains a workflowAttributes object with all key-value pairs

#### Scenario: Update job with workflow attributes included
- **WHEN** user saves changes to an existing job order that has workflow attribute values
- **THEN** the HTTP PUT request body contains a workflowAttributes object with updated key-value pairs

### Requirement: Backend DTOs accept workflow attributes
The CreateJobOrderRequest and UpdateJobOrderRequest C# models SHALL include a property to receive workflow attribute data from the frontend.

#### Scenario: Create request deserializes workflow attributes
- **WHEN** API receives a POST request with workflowAttributes in JSON body
- **THEN** CreateJobOrderRequest.WorkflowAttributes contains the deserialized dictionary

#### Scenario: Update request deserializes workflow attributes
- **WHEN** API receives a PUT request with workflowAttributes in JSON body
- **THEN** UpdateJobOrderRequest.WorkflowAttributes contains the deserialized dictionary

### Requirement: Workflow attributes persist to database
The EfJobManagementRepository SHALL write workflow attribute values to the JobWorkflow table when creating or updating job orders, following the same pattern as the legacy SaveWorkflow method in JobRecord.cs.

#### Column mapping
Each workflow attribute key-value pair SHALL be persisted as a JobWorkflow row:
- `OrderId` = the job order's ID
- `WorkflowId` = resolved from `Z_OrderTypeWorkflow` by matching `(OrderType, WorkflowName)` where `WorkflowName` is the attribute key
- `WorkIndex` = the `WorkIndex` value from the matching `Z_OrderTypeWorkflow` row
- `WorkTitle` = the user's selected value (dictionary value)
- `WorkStatus` = null (discriminator from real workflow steps)
- `WorkInstruction` = null, `WorkNotes` = null

#### Upsert semantics
The repository SHALL follow the legacy upsert pattern: load existing `JobWorkflow` by `(OrderId, WorkflowId)`; if found, update it; if not found, insert a new row.

#### Scenario: New job order persists workflow attributes
- **WHEN** CreateJobOrder is called with non-empty workflowAttributes dictionary
- **THEN** corresponding rows are inserted into JobWorkflow table with correct OrderId, resolved WorkflowId, WorkIndex, and WorkTitle values

#### Scenario: Existing job order updates workflow attributes
- **WHEN** UpdateJobOrder is called with modified workflowAttributes dictionary
- **THEN** existing JobWorkflow rows for that OrderId are updated or new ones inserted, using the (OrderId, WorkflowId) upsert key

#### Scenario: Unknown attribute name is skipped
- **WHEN** a dictionary key does not match any Z_OrderTypeWorkflow.WorkflowName for the job's OrderType
- **THEN** that entry is silently skipped and a warning is logged

### Requirement: API responses include workflow attributes
The JobDetail response model SHALL include workflowAttributes so the frontend can load them during edit operations. List responses (JobOrderRecord) SHALL NOT include workflowAttributes, as the `vwOrderDetailList` view has no workflow data.

#### Scenario: Get job detail returns workflow attributes
- **WHEN** frontend calls GET /api/job-orders/{id} for a job with saved workflow attributes
- **THEN** response includes workflowAttributes object with all stored key-value pairs

#### Scenario: Job detail without workflow attributes returns empty dictionary
- **WHEN** frontend calls GET /api/job-orders/{id} for a job that has no JobWorkflow rows with WorkStatus=null
- **THEN** response includes workflowAttributes as an empty dictionary `{}`

### Requirement: Frontend hydrates existing workflow attributes in edit mode
The buildDraft function in JobOrderForm.vue SHALL map incoming workflowAttributes from JobDetail into local form state when loading an existing job order.

#### Scenario: Edit mode loads saved workflow values
- **WHEN** user opens a job order that has previously saved workflow attribute values
- **THEN** the form fields for workflow attributes display the saved values correctly

# Legacy EF6 Inventory (Tasks 1-5)

## Scope and Sources

- Legacy root scanned: `C:\Projects\JB2015`
- EF model sources:
  - `C:\Projects\JB2015\JB5.EF6\JB5Model.edmx`
  - `C:\Projects\JB2015\JB5.API\Models\JB5ApiModel.edmx`
- Legacy DAL source scanned: `C:\Projects\JB2015\Job.Book.DAL\*.cs`

## 1) EF6 DbContexts and EDMX models

### DbContexts

- `JB5.EF6.JB5Entities` in `C:\Projects\JB2015\JB5.EF6\JB5Model.Context.cs`
- `JB5.API.Models.JB5ApiEntities` in `C:\Projects\JB2015\JB5.API\Models\JB5ApiModel.Context.cs`

### EDMX Models

- `C:\Projects\JB2015\JB5.EF6\JB5Model.edmx`
- `C:\Projects\JB2015\JB5.API\Models\JB5ApiModel.edmx`

## 2) Stored procedure function imports and signatures

### EF6 EDMX function imports

- `JB5Model.edmx`: `FunctionImport = 0`
- `JB5ApiModel.edmx`: `FunctionImport = 0`

No EDMX function-import mappings were found. Legacy stored procedure usage is implemented in `Job.Book.DAL` through helper wrappers.

### Legacy DAL stored procedure patterns (wrapper signatures)

These signatures are used repeatedly across entity DAL classes:

- Read single: `ExecuteReader("sp<Entity>_SelRec", params object[] parameterValues)`
- Read collection: `ExecuteReader("sp<Entity>_SelAll", params object[] parameterValues)`
- Insert: `ExecuteNonQuery("sp<Entity>_InsRec", "@<PrimaryKey>", out object returnedKeyValue, params object[] parameterValues)`
- Update: `ExecuteNonQuery("sp<Entity>_UpdRec", params object[] parameterValues)`
- Delete: `ExecuteNonQuery("sp<Entity>_DelRec", params object[] parameterValues)`

### Stored procedure names discovered in legacy DAL (unique)

- Customers: `spCustomers_SelRec`, `spCustomers_SelAll`, `spCustomers_InsRec`, `spCustomers_UpdRec`, `spCustomers_DelRec`
- InvoiceHeader: `spInvoiceHeader_SelRec`, `spInvoiceHeader_SelAll`, `spInvoiceHeader_InsRec`, `spInvoiceHeader_UpdRec`, `spInvoiceHeader_DelRec`
- InvoiceItems: `spInvoiceItems_SelRec`, `spInvoiceItems_SelAll`, `spInvoiceItems_InsRec`, `spInvoiceItems_UpdRec`, `spInvoiceItems_DelRec`
- InvoiceSubItems: `spInvoiceSubItems_SelRec`, `spInvoiceSubItems_SelAll`, `spInvoiceSubItems_InsRec`, `spInvoiceSubItems_UpdRec`, `spInvoiceSubItems_DelRec`
- JobAttachment: `spJobAttachment_SelRec`, `spJobAttachment_SelAll`, `spJobAttachment_InsRec`, `spJobAttachment_UpdRec`, `spJobAttachment_DelRec`
- JobOrder: `spJobOrder_SelRec`, `spJobOrder_SelAll`, `spJobOrder_InsRec`, `spJobOrder_UpdRec`, `spJobOrder_DelRec`
- JobPackingOnAir: `spJobPackingOnAir_SelRec`, `spJobPackingOnAir_SelAll`, `spJobPackingOnAir_InsRec`, `spJobPackingOnAir_UpdRec`, `spJobPackingOnAir_DelRec`
- JobSchedule: `spJobSchedule_SelRec`, `spJobSchedule_SelAll`, `spJobSchedule_InsRec`, `spJobSchedule_UpdRec`, `spJobSchedule_DelRec`
- JobWorkflow: `spJobWorkflow_SelRec`, `spJobWorkflow_SelAll`, `spJobWorkflow_InsRec`, `spJobWorkflow_UpdRec`, `spJobWorkflow_DelRec`
- JobWorkflowForms: `spJobWorkflowForms_SelRec`, `spJobWorkflowForms_SelAll`, `spJobWorkflowForms_InsRec`, `spJobWorkflowForms_UpdRec`, `spJobWorkflowForms_DelRec`
- Product: `spProduct_SelRec`, `spProduct_SelAll`, `spProduct_InsRec`, `spProduct_UpdRec`, `spProduct_DelRec`
- ProductAttachment: `spProductAttachment_SelRec`, `spProductAttachment_SelAll`, `spProductAttachment_InsRec`, `spProductAttachment_UpdRec`, `spProductAttachment_DelRec`
- SmlRtfExtractToDN: `spSmlRtfExtractToDN_SelRec`, `spSmlRtfExtractToDN_SelAll`, `spSmlRtfExtractToDN_InsRec`, `spSmlRtfExtractToDN_UpdRec`, `spSmlRtfExtractToDN_DelRec`
- SmlRtfHeader: `spSmlRtfHeader_SelRec`, `spSmlRtfHeader_SelAll`, `spSmlRtfHeader_InsRec`, `spSmlRtfHeader_UpdRec`, `spSmlRtfHeader_DelRec`
- SmlRtfItems: `spSmlRtfItems_SelRec`, `spSmlRtfItems_SelAll`, `spSmlRtfItems_InsRec`, `spSmlRtfItems_UpdRec`, `spSmlRtfItems_DelRec`
- SmlRtfSubItems: `spSmlRtfSubItems_SelRec`, `spSmlRtfSubItems_SelAll`, `spSmlRtfSubItems_InsRec`, `spSmlRtfSubItems_UpdRec`, `spSmlRtfSubItems_DelRec`
- StockInOut: `spStockInOut_SelRec`, `spStockInOut_SelAll`, `spStockInOut_InsRec`, `spStockInOut_UpdRec`, `spStockInOut_DelRec`
- Supplier: `spSupplier_SelRec`, `spSupplier_SelAll`, `spSupplier_InsRec`, `spSupplier_UpdRec`, `spSupplier_DelRec`
- SystemInfo: `spSystemInfo_SelRec`, `spSystemInfo_SelAll`, `spSystemInfo_InsRec`, `spSystemInfo_UpdRec`, `spSystemInfo_DelRec`
- UserInfo: `spUserInfo_SelRec`, `spUserInfo_SelAll`, `spUserInfo_InsRec`, `spUserInfo_UpdRec`, `spUserInfo_DelRec`
- Z_Category: `spZ_Category_SelRec`, `spZ_Category_SelAll`, `spZ_Category_InsRec`, `spZ_Category_UpdRec`, `spZ_Category_DelRec`
- Z_Forms: `spZ_Forms_SelRec`, `spZ_Forms_SelAll`, `spZ_Forms_InsRec`, `spZ_Forms_UpdRec`, `spZ_Forms_DelRec`
- Z_OrderTypeWorkflow: `spZ_OrderTypeWorkflow_SelRec`, `spZ_OrderTypeWorkflow_SelAll`, `spZ_OrderTypeWorkflow_InsRec`, `spZ_OrderTypeWorkflow_UpdRec`, `spZ_OrderTypeWorkflow_DelRec`
- Z_Workflow: `spZ_Workflow_SelRec`, `spZ_Workflow_SelAll`, `spZ_Workflow_InsRec`, `spZ_Workflow_UpdRec`, `spZ_Workflow_DelRec`
- Z_WorkflowForms: `spZ_WorkflowForms_SelRec`, `spZ_WorkflowForms_SelAll`, `spZ_WorkflowForms_InsRec`, `spZ_WorkflowForms_UpdRec`, `spZ_WorkflowForms_DelRec`

## 3) Complex types and table-valued functions

Namespace-agnostic EDMX inspection results:

- `JB5Model.edmx`: `ComplexType = 0`, `Function = 0`, `DefiningQuery = 17`
- `JB5ApiModel.edmx`: `ComplexType = 0`, `Function = 0`, `DefiningQuery = 3`

No EDMX complex types or SSDL function definitions (including TVFs) were found. Read-only modelled views exist via `DefiningQuery` entries.

## 4) Optimistic concurrency tokens

Search markers checked in both EDMX files:

- `ConcurrencyMode="Fixed"`
- `StoreGeneratedPattern="Computed"`
- `Type="timestamp"`
- `Type="rowversion"`

Result: no concurrency-token markers found in either EDMX.

Implication for migration test planning:

- EF6 metadata does not currently advertise rowversion/timestamp optimistic concurrency tokens.
- Migration should treat optimistic concurrency coverage as "none identified from EF6 metadata" unless database-level inspection reveals additional constraints.

## 5) LINQ queries relying on EF6 lazy-loading behavior

### Evidence of lazy-loading-capable entity shapes

Generated entity classes in both model sets expose `virtual ICollection<T>` navigation properties (for example, `JobOrder.JobSchedule`, `JobOrder.JobWorkflow`, `Customers.InvoiceHeader`).

### API query hotspots without explicit Include chains

- `JobOrdersController.GetJobOrder()` returns `IQueryable<JobOrder>` directly (`db.JobOrder.Take(100)`) with no `Include`.
- `JobOrdersController.GetJobOrder(Guid id)` loads via `FindAsync` and returns entity directly.
- `PrintsController` issues LINQ queries over `ctx.JobSchedule` and view-backed sets without explicit include expansion.

### Migration note

Because EF Core does not enable lazy-loading by default, endpoints returning entities with virtual navigation properties must be reviewed and migrated to explicit query-shape loading (`Include` / projections) where nested data is expected.

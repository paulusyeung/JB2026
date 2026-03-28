# Stored Procedure Slices: JobAttachment, JobSchedule, JobOrder, JobPackingOnAir, Product, Supplier, ProductAttachment, StockInOut, Customer, InvoiceHeader, InvoiceItems, InvoiceSubItems, JobWorkflow, JobWorkflowForms, Z_Category, Z_Forms, Z_Workflow, Z_WorkflowForms, Z_OrderTypeWorkflow, SmlRtfHeader, SmlRtfItems, SmlRtfSubItems, SmlRtfExtractToDN, UserInfo, SystemInfo (Option 1 Subset)

## Scope

This subset currently implements and validates the following stored procedure families:

### JobAttachment

- `spJobAttachment_SelRec`
- `spJobAttachment_InsRec`
- `spJobAttachment_UpdRec`
- `spJobAttachment_DelRec`

### JobSchedule

- `spJobSchedule_SelRec`
- `spJobSchedule_InsRec`
- `spJobSchedule_UpdRec`
- `spJobSchedule_DelRec`

### JobOrder

- `spJobOrder_SelRec`
- `spJobOrder_InsRec`
- `spJobOrder_UpdRec`
- `spJobOrder_DelRec`

### JobPackingOnAir

- `spJobPackingOnAir_SelRec`
- `spJobPackingOnAir_InsRec`
- `spJobPackingOnAir_UpdRec`
- `spJobPackingOnAir_DelRec`

### Product

- `spProduct_SelRec`
- `spProduct_InsRec`
- `spProduct_UpdRec`
- `spProduct_DelRec`

### Supplier

- `spSupplier_SelRec`
- `spSupplier_InsRec`
- `spSupplier_UpdRec`
- `spSupplier_DelRec`

### ProductAttachment

- `spProductAttachment_SelRec`
- `spProductAttachment_InsRec`
- `spProductAttachment_UpdRec`
- `spProductAttachment_DelRec`

### StockInOut

- `spStockInOut_SelRec`
- `spStockInOut_InsRec`
- `spStockInOut_UpdRec`
- `spStockInOut_DelRec`

### Customer

- `spCustomers_SelRec`
- `spCustomers_InsRec`
- `spCustomers_UpdRec`
- `spCustomers_DelRec`

### InvoiceHeader

- `spInvoiceHeader_SelRec`
- `spInvoiceHeader_InsRec`
- `spInvoiceHeader_UpdRec`
- `spInvoiceHeader_DelRec`

### InvoiceItems

- `spInvoiceItems_SelRec`
- `spInvoiceItems_InsRec`
- `spInvoiceItems_UpdRec`
- `spInvoiceItems_DelRec`

### InvoiceSubItems

- `spInvoiceSubItems_SelRec`
- `spInvoiceSubItems_InsRec`
- `spInvoiceSubItems_UpdRec`
- `spInvoiceSubItems_DelRec`

### JobWorkflow

- `spJobWorkflow_SelRec`
- `spJobWorkflow_InsRec`
- `spJobWorkflow_UpdRec`
- `spJobWorkflow_DelRec`

### JobWorkflowForms

- `spJobWorkflowForms_SelRec`
- `spJobWorkflowForms_InsRec`
- `spJobWorkflowForms_UpdRec`
- `spJobWorkflowForms_DelRec`

### Z_Category

- `spZ_Category_SelRec`
- `spZ_Category_InsRec`
- `spZ_Category_UpdRec`
- `spZ_Category_DelRec`

### Z_Forms

- `spZ_Forms_SelRec`
- `spZ_Forms_InsRec`
- `spZ_Forms_UpdRec`
- `spZ_Forms_DelRec`

### Z_Workflow

- `spZ_Workflow_SelRec`
- `spZ_Workflow_InsRec`
- `spZ_Workflow_UpdRec`
- `spZ_Workflow_DelRec`

### Z_WorkflowForms

- `spZ_WorkflowForms_SelRec`
- `spZ_WorkflowForms_InsRec`
- `spZ_WorkflowForms_UpdRec`
- `spZ_WorkflowForms_DelRec`

### Z_OrderTypeWorkflow

- `spZ_OrderTypeWorkflow_SelRec`
- `spZ_OrderTypeWorkflow_InsRec`
- `spZ_OrderTypeWorkflow_UpdRec`
- `spZ_OrderTypeWorkflow_DelRec`

### SmlRtfHeader

- `spSmlRtfHeader_SelRec`
- `spSmlRtfHeader_InsRec`
- `spSmlRtfHeader_UpdRec`
- `spSmlRtfHeader_DelRec`

### SmlRtfItems

- `spSmlRtfItems_SelRec`
- `spSmlRtfItems_InsRec`
- `spSmlRtfItems_UpdRec`
- `spSmlRtfItems_DelRec`

### SmlRtfSubItems

- `spSmlRtfSubItems_SelRec`
- `spSmlRtfSubItems_InsRec`
- `spSmlRtfSubItems_UpdRec`
- `spSmlRtfSubItems_DelRec`

### SmlRtfExtractToDN

- `spSmlRtfExtractToDN_SelRec`
- `spSmlRtfExtractToDN_InsRec`
- `spSmlRtfExtractToDN_UpdRec`
- `spSmlRtfExtractToDN_DelRec`

### UserInfo

- `spUserInfo_SelRec`
- `spUserInfo_InsRec`
- `spUserInfo_UpdRec`
- `spUserInfo_DelRec`

### SystemInfo

- `spSystemInfo_SelRec`
- `spSystemInfo_InsRec`
- `spSystemInfo_UpdRec`
- `spSystemInfo_DelRec`

## Implementation

- Added gateway contract and request/response records:
  - `JB2026.Api/Services/IJobAttachmentStoredProcedureGateway.cs`
  - `JB2026.Api/Services/IJobScheduleStoredProcedureGateway.cs`
  - `JB2026.Api/Services/IJobOrderStoredProcedureGateway.cs`
  - `JB2026.Api/Services/IJobPackingOnAirStoredProcedureGateway.cs`
  - `JB2026.Api/Services/IProductStoredProcedureGateway.cs`
  - `JB2026.Api/Services/ISupplierStoredProcedureGateway.cs`
  - `JB2026.Api/Services/IProductAttachmentStoredProcedureGateway.cs`
  - `JB2026.Api/Services/IStockInOutStoredProcedureGateway.cs`
  - `JB2026.Api/Services/ICustomerStoredProcedureGateway.cs`
  - `JB2026.Api/Services/IInvoiceHeaderStoredProcedureGateway.cs`
  - `JB2026.Api/Services/IInvoiceItemStoredProcedureGateway.cs`
  - `JB2026.Api/Services/IInvoiceSubItemStoredProcedureGateway.cs`
  - `JB2026.Api/Services/IJobWorkflowStoredProcedureGateway.cs`
  - `JB2026.Api/Services/IJobWorkflowFormStoredProcedureGateway.cs`
  - `JB2026.Api/Services/IZCategoryStoredProcedureGateway.cs`
  - `JB2026.Api/Services/IZFormStoredProcedureGateway.cs`
  - `JB2026.Api/Services/IZWorkflowStoredProcedureGateway.cs`
  - `JB2026.Api/Services/IZWorkflowFormStoredProcedureGateway.cs`
  - `JB2026.Api/Services/IZOrderTypeWorkflowStoredProcedureGateway.cs`
  - `JB2026.Api/Services/ISmlRtfHeaderStoredProcedureGateway.cs`
  - `JB2026.Api/Services/ISmlRtfItemStoredProcedureGateway.cs`
  - `JB2026.Api/Services/ISmlRtfSubItemStoredProcedureGateway.cs`
  - `JB2026.Api/Services/ISmlRtfExtractToDNStoredProcedureGateway.cs`
  - `JB2026.Api/Services/IUserInfoStoredProcedureGateway.cs`
  - `JB2026.Api/Services/ISystemInfoStoredProcedureGateway.cs`
- Added parameterized `DbCommand` implementation:
  - `JB2026.Api/Services/JobAttachmentStoredProcedureGateway.cs`
  - `JB2026.Api/Services/JobScheduleStoredProcedureGateway.cs`
  - `JB2026.Api/Services/JobOrderStoredProcedureGateway.cs`
  - `JB2026.Api/Services/JobPackingOnAirStoredProcedureGateway.cs`
  - `JB2026.Api/Services/ProductStoredProcedureGateway.cs`
  - `JB2026.Api/Services/SupplierStoredProcedureGateway.cs`
  - `JB2026.Api/Services/ProductAttachmentStoredProcedureGateway.cs`
  - `JB2026.Api/Services/StockInOutStoredProcedureGateway.cs`
  - `JB2026.Api/Services/CustomerStoredProcedureGateway.cs`
  - `JB2026.Api/Services/InvoiceHeaderStoredProcedureGateway.cs`
  - `JB2026.Api/Services/InvoiceItemStoredProcedureGateway.cs`
  - `JB2026.Api/Services/InvoiceSubItemStoredProcedureGateway.cs`
  - `JB2026.Api/Services/JobWorkflowStoredProcedureGateway.cs`
  - `JB2026.Api/Services/JobWorkflowFormStoredProcedureGateway.cs`
  - `JB2026.Api/Services/ZCategoryStoredProcedureGateway.cs`
  - `JB2026.Api/Services/ZFormStoredProcedureGateway.cs`
  - `JB2026.Api/Services/ZWorkflowStoredProcedureGateway.cs`
  - `JB2026.Api/Services/ZWorkflowFormStoredProcedureGateway.cs`
  - `JB2026.Api/Services/ZOrderTypeWorkflowStoredProcedureGateway.cs`
  - `JB2026.Api/Services/SmlRtfHeaderStoredProcedureGateway.cs`
  - `JB2026.Api/Services/SmlRtfItemStoredProcedureGateway.cs`
  - `JB2026.Api/Services/SmlRtfSubItemStoredProcedureGateway.cs`
  - `JB2026.Api/Services/SmlRtfExtractToDNStoredProcedureGateway.cs`
  - `JB2026.Api/Services/UserInfoStoredProcedureGateway.cs`
  - `JB2026.Api/Services/SystemInfoStoredProcedureGateway.cs`
- Registered gateway in API DI when DB connection exists:
  - `JB2026.Api/Program.cs`

Implementation details:

- Uses `CommandType.StoredProcedure` for all calls.
- Uses typed parameters only (no SQL string concatenation).
- Handles `SET NOCOUNT ON` procedures safely.

## Output-Comparison Parity Tests

- Added parity tests:
  - `JB2026.Api.ParityTests/JobAttachmentStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/JobScheduleStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/JobOrderStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/JobPackingOnAirStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/ProductStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/SupplierStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/ProductAttachmentStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/StockInOutStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/CustomerStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/InvoiceHeaderStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/InvoiceItemStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/InvoiceSubItemStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/JobWorkflowStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/JobWorkflowFormStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/ZCategoryStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/ZFormStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/ZWorkflowStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/ZWorkflowFormStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/ZOrderTypeWorkflowStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/SmlRtfHeaderStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/SmlRtfItemStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/SmlRtfSubItemStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/SmlRtfExtractToDNStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/UserInfoStoredProcedureParityTests.cs`
  - `JB2026.Api.ParityTests/SystemInfoStoredProcedureParityTests.cs`

Covered checks:

- Insert + select via proc matches table state (`JobAttachment`).
- Update via proc matches table state (`JobAttachment`).
- Insert + select via proc matches table state (`JobSchedule`).
- Update via proc matches table state (`JobSchedule`).
- Insert + select via proc matches table state (`JobOrder`).
- Update via proc matches table state (`JobOrder`).
- Insert + select via proc matches table state (`JobPackingOnAir`).
- Update via proc matches table state (`JobPackingOnAir`).
- Insert + select via proc matches table state (`Product`).
- Update via proc matches table state (`Product`).
- Insert + select via proc matches table state (`Supplier`).
- Update via proc matches table state (`Supplier`).
- Insert + select via proc matches table state (`ProductAttachment`).
- Update via proc matches table state (`ProductAttachment`).
- Insert + select via proc matches table state (`StockInOut`).
- Update via proc matches table state (`StockInOut`).
- Insert + select via proc matches table state (`Customer`).
- Update via proc matches table state (`Customer`).
- Insert + select via proc matches table state (`InvoiceHeader`).
- Update via proc matches table state (`InvoiceHeader`).
- Insert + select via proc matches table state (`InvoiceItems`).
- Update via proc matches table state (`InvoiceItems`).
- Insert + select via proc matches table state (`InvoiceSubItems`).
- Update via proc matches table state (`InvoiceSubItems`).
- Insert + select via proc matches table state (`JobWorkflow`).
- Update via proc matches table state (`JobWorkflow`).
- Insert + select via proc matches table state (`JobWorkflowForms`).
- Update via proc matches table state (`JobWorkflowForms`).
- Insert + select via proc matches table state (`Z_Category`).
- Update via proc matches table state (`Z_Category`).
- Insert + select via proc matches table state (`Z_Forms`).
- Update via proc matches table state (`Z_Forms`).
- Insert + select via proc matches table state (`Z_Workflow`).
- Update via proc matches table state (`Z_Workflow`).
- Insert + select via proc matches table state (`Z_WorkflowForms`).
- Update via proc matches table state (`Z_WorkflowForms`).
- Insert + select via proc matches table state (`Z_OrderTypeWorkflow`).
- Update via proc matches table state (`Z_OrderTypeWorkflow`).
- Insert + select via proc matches table state (`SmlRtfHeader`).
- Update via proc matches table state (`SmlRtfHeader`).
- Insert + select via proc matches table state (`SmlRtfItems`).
- Update via proc matches table state (`SmlRtfItems`).
- Insert + select via proc matches table state (`SmlRtfSubItems`).
- Update via proc matches table state (`SmlRtfSubItems`).
- Insert + select via proc matches table state (`SmlRtfExtractToDN`).
- Update via proc matches table state (`SmlRtfExtractToDN`).
- Insert + select via proc matches table state (`UserInfo`).
- Update via proc matches table state (`UserInfo`).
- Insert + select via proc matches table state (`SystemInfo`).
- Update via proc matches table state (`SystemInfo`).

## Validation Commands

- `dotnet build .\JB2026.Api\JB2026.Api.csproj -c Release`
- `dotnet test .\JB2026.Api.ParityTests\JB2026.Api.ParityTests.csproj -c Release --filter "FullyQualifiedName~JobScheduleStoredProcedureParityTests|FullyQualifiedName~JobOrderStoredProcedureParityTests|FullyQualifiedName~JobAttachmentStoredProcedureParityTests|FullyQualifiedName~JobPackingOnAirStoredProcedureParityTests|FullyQualifiedName~ProductStoredProcedureParityTests|FullyQualifiedName~SupplierStoredProcedureParityTests|FullyQualifiedName~ProductAttachmentStoredProcedureParityTests|FullyQualifiedName~StockInOutStoredProcedureParityTests|FullyQualifiedName~CustomerStoredProcedureParityTests|FullyQualifiedName~InvoiceHeaderStoredProcedureParityTests|FullyQualifiedName~InvoiceItemStoredProcedureParityTests|FullyQualifiedName~InvoiceSubItemStoredProcedureParityTests|FullyQualifiedName~JobWorkflowStoredProcedureParityTests|FullyQualifiedName~JobWorkflowFormStoredProcedureParityTests|FullyQualifiedName~ZCategoryStoredProcedureParityTests|FullyQualifiedName~ZFormStoredProcedureParityTests|FullyQualifiedName~ZWorkflowStoredProcedureParityTests|FullyQualifiedName~ZWorkflowFormStoredProcedureParityTests|FullyQualifiedName~ZOrderTypeWorkflowStoredProcedureParityTests|FullyQualifiedName~SmlRtfHeaderStoredProcedureParityTests|FullyQualifiedName~SmlRtfItemStoredProcedureParityTests|FullyQualifiedName~SmlRtfSubItemStoredProcedureParityTests|FullyQualifiedName~SmlRtfExtractToDNStoredProcedureParityTests|FullyQualifiedName~UserInfoStoredProcedureParityTests|FullyQualifiedName~SystemInfoStoredProcedureParityTests"`

Result:

- 50/50 targeted stored-procedure parity tests passed for this subset.

## Notes

- This is a bounded subset for Option 1.
- Group 3 tasks remain open until equivalent coverage is implemented across the broader stored-procedure set.

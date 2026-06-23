## 1. Backend — New API Endpoint

- [x] 1.1 Create `OrderTypeWorkflowAttribute` DTO with fields `WorkIndex`, `WorkflowName`, `WorkTitle`, and `Options` (parsed list)
- [x] 1.2 Create `OrderTypeWorkflowAttributeResponse` envelope DTO containing `WorkflowAttributes` list
- [x] 1.3 Add stored procedure gateway or EF Core query to fetch `Z_OrderTypeWorkflow` joined with `Z_Workflow` and `Z_WorkflowForms`, ordered by `OrderType` then `WorkIndex`
- [x] 1.4 Create `JobOrderTypesController` (or extend existing) with `GET /api/v2/order-types/{orderType}/workflow-attributes` action
- [x] 1.5 Verify controller compiles

## 2. Frontend — TypeScript Types and Service

- [x] 2.1 Add `OrderTypeWorkflowAttribute` interface to `types/api.ts`
- [x] 2.2 Add `workflowAttributes: Record<string, string>` to `JobOrderFormData`
- [x] 2.3 Create `getOrderTypeWorkflowAttributes(orderType: number)` service function in `services/jobOrders.ts`

## 3. Frontend — JobOrderForm.vue Dynamic Rendering

- [x] 3.1 Add reactive `workflowAttributeDefs` ref and `workflowAttributeValues` ref (replacing `legacyPrintingPaper`, `legacyFinishingOutput`, `legacyPackagingRequirement`)
- [x] 3.2 Add `watch(() => draft.value.orderType, ...)` that resets `workflowAttributeValues`, fetches `getOrderTypeWorkflowAttributes`, and stores into `workflowAttributeDefs`
- [x] 3.3 Replace the static 3-row `legacy-attribute-grid` with a `v-for` loop over `workflowAttributeDefs`, rendering `v-select` for each row with `workflowName` as label and `options` as items
- [x] 3.4 Add indicator color CSS classes (`legacy-indicator-orange`, `legacy-indicator-purple`, `legacy-indicator-teal`) and compute color class by row index cycling through palette
- [x] 3.5 Bind `v-model` of each `v-select` to `workflowAttributeValues[def.workflowName]`

## 4. Cleanup

- [x] 4.1 Remove `legacyPrintingPaper`, `legacyFinishingOutput`, `legacyPackagingRequirement` refs
- [x] 4.2 Remove `legacyAttributeOptions` computed property
- [x] 4.3 Remove unused `paymentTermsOptions` computed (was only used by `legacyAttributeOptions`)

## 5. Verification

- [x] 5.1 Verify OrderType 0 (Printing) renders 3 rows with correct labels and options
- [x] 5.2 Verify OrderType 1 (Printed Label) renders 2 rows
- [x] 5.3 Verify OrderType 2 (Woven Label) renders 3 rows
- [x] 5.4 Verify OrderType 3 (Others) renders 2 rows
- [x] 5.5 Verify changing order type clears previous selections and fetches new attributes
- [x] 5.6 Verify selected values are preserved in form draft across tab switches
- [x] 5.7 Verify no console errors on the form page

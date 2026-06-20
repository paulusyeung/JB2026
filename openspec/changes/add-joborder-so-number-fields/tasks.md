## 1. Backend DTOs

- [ ] 1.1 Add `string? SONumber [StringLength(32)]` and `string? OriginalSONumber [StringLength(32)]` to `CreateJobOrderRequest.cs`.
- [ ] 1.2 Add `string? SONumber [StringLength(32)]` and `string? OriginalSONumber [StringLength(32)]` to `UpdateJobOrderRequest.cs`.
- [ ] 1.3 Add `string? SONumber` and `string? OriginalSONumber` to `JobOrderResponse.cs`.
- [ ] 1.4 Add `string? SONumber` and `string? OriginalSONumber` to `JobDetailResponse.cs`.

## 2. Repository mapping

- [ ] 2.1 Update `EfJobManagementRepository.MapOrder(JobOrder)` to map `job.SONumber` and `job.OriginalSONumber`.
- [ ] 2.2 Update `EfJobManagementRepository.MapOrder(vwOrderDetailList)` to map `order.SONumber`.
- [ ] 2.3 Update `EfJobManagementRepository.CreateJobOrder` to set entity fields from request.
- [ ] 2.4 Update `EfJobManagementRepository.UpdateJobOrder` to copy fields from request to entity.
- [ ] 2.5 Update `InMemoryJobManagementRepository` — add fields to `JobRecord`, update `CreateJobOrder`, `UpdateJobOrder`, `MapOrder`.

## 3. Test stubs

- [ ] 3.1 Update `JobOrdersControllerTests.StubRepository.CreateResponse()` to include new fields.
- [ ] 3.2 Update `JobsControllerTests.StubRepository.CreateJobOrder()` and `UpdateJobOrder()` to include new fields.

## 4. TypeScript types

- [ ] 4.1 Add `soNumber?: string` and `originalSONumber?: string` to `JobOrderFormData` in `src/types/api.ts`.
- [ ] 4.2 Add `soNumber?: string` and `originalSONumber?: string` to `JobOrderRecord` in `src/types/api.ts`.
- [ ] 4.3 Add `soNumber?: string` and `originalSONumber?: string` to `JobDetail` in `src/types/api.ts`.

## 5. i18n labels

- [ ] 5.1 Add `salesOrderNumber` and `originalSalesOrderNumber` to `src/i18n/locales/en/jobForm.ts`.
- [ ] 5.2 Add corresponding translations to `src/i18n/locales/zhHans/jobForm.ts`.
- [ ] 5.3 Add corresponding translations to `src/i18n/locales/zhHant/jobForm.ts`.

## 6. Service layer mapping

- [ ] 6.1 Update `CreateJobRequest` interface in `src/services/jobs.ts` to include `soNumber` and `originalSONumber`.
- [ ] 6.2 Update `UpdateJobRequest` interface in `src/services/jobs.ts` to include `soNumber` and `originalSONumber`.
- [ ] 6.3 Ensure the `saveJob()` function maps these fields from `JobOrderFormData` into the request payloads.
- [ ] 6.4 Update `CreateJobOrderRequest` and `UpdateJobOrderRequest` in `src/services/jobOrders.ts` to include fields.

## 7. Form component

- [ ] 7.1 Add two readonly `v-text-field` inputs for Sales Order Number and Original Sales Order Number to `JobOrderForm.vue`.
- [ ] 7.2 Bind the fields to `draft.value.soNumber` and `draft.value.originalSONumber`.
- [ ] 7.3 Update the `buildDraft()` function in `JobOrderForm.vue` to include the new fields when loading job data.
- [ ] 7.4 Verify the form layout accommodates the new fields without breaking existing structure.

## 8. Testing & validation

- [ ] 8.1 Update parity test stubs to include new fields (all existing tests must pass).
- [ ] 8.2 Verify that saving a job with SO numbers persists correctly (manual integration test or existing parity tests).
- [ ] 8.3 Confirm i18n keys resolve correctly in all three locales.
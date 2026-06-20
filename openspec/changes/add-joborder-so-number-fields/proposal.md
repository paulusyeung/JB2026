## Why

The Job Order form currently lacks input fields for **Sales Order Number** and **Original Sales Order Number**. These fields exist in the `JobOrder` EF entity and the stored procedure gateway, but the API DTOs (`CreateJobOrderRequest`, `UpdateJobOrderRequest`, `JobOrderResponse`, `JobDetailResponse`) and repository mapping layer do not expose them, and the Vuetify form has no corresponding inputs.

## What Changes

- Add `SONumber` and `OriginalSONumber` to all API DTOs (`CreateJobOrderRequest`, `UpdateJobOrderRequest`, `JobOrderResponse`, `JobDetailResponse`).
- Update `EfJobManagementRepository` (both `MapOrder` overloads, `CreateJobOrder`, `UpdateJobOrder`) and `InMemoryJobManagementRepository` to map the new fields.
- Add two new readonly input fields to `JobOrderForm.vue`: **Sales Order Number** and **Original Sales Order Number**.
- Extend the TypeScript type definitions (`JobOrderFormData`, `JobOrderRecord`, `JobDetail`) to include `soNumber` and `originalSONumber`.
- Update the frontend service layer to map `soNumber` / `originalSONumber` through create/update payloads.
- Add localized labels for the new fields across all three locales (en, zhHans, zhHant).
- Wire the fields into the form draft builders so they round-trip correctly on load and save.

## Capabilities

### New Capabilities
- `job-order-so-number-inputs`: Display and edit Sales Order Number and Original Sales Order Number in the Job Order form with full i18n support.

### Modified Capabilities
- None (additive change only).

## Impact

- Affected backend: `Models/CreateJobOrderRequest.cs`, `Models/UpdateJobOrderRequest.cs`, `Models/JobOrderResponse.cs`, `Models/JobDetailResponse.cs`, `Services/EfJobManagementRepository.cs`, `Services/InMemoryJobManagementRepository.cs`.
- Affected frontend: `src/components/forms/JobOrderForm.vue`, `src/types/api.ts`, `src/services/jobs.ts`, `src/services/jobOrders.ts`.
- Affected i18n: `src/i18n/locales/en/jobForm.ts`, `src/i18n/locales/zhHans/jobForm.ts`, `src/i18n/locales/zhHant/jobForm.ts`.
- Backend: No schema or stored-procedure changes required (DB columns already exist). API DTOs and repository mappings updated.
- Tests: Update parity test stubs to include new fields; add frontend unit coverage for new fields in form serialization.
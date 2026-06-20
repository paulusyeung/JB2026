## Context

The Job Order form (`JobOrderForm.vue`) is a Vuetify 3 replacement for the legacy DevExpress job order editor. The backend `JobOrder` EF entity already has `SONumber` and `OriginalSONumber` columns (nullable strings, max length 32). The stored procedure gateway in `JobOrderStoredProcedureGateway.cs` reads and writes these fields correctly.

However, the API DTO layer does NOT expose these fields: `CreateJobOrderRequest`, `UpdateJobOrderRequest`, `JobOrderResponse`, and `JobDetailResponse` all lack `SONumber`/`OriginalSONumber`. The EF repository's `MapOrder()` overloads do not read them, and `CreateJobOrder()` / `UpdateJobOrder()` do not write them. The InMemory test repository has the same gap.

The gap therefore spans both backend (DTOs + repository mapping) and frontend (TypeScript types, service layer, form inputs). DB and stored procedure layers require no changes.

## Goals / Non-Goals

**Goals:**
- Provide two new readonly input fields (Sales Order Number, Original Sales Order Number) in the Job Order form.
- Ensure the fields populate correctly when loading a job and serialize back on save.
- Add proper i18n labels in all three supported locales.
- Maintain existing form behavior and validation rules.

**Non-Goals:**
- Making the fields editable (they remain readonly to match legacy behavior).
- Adding backend endpoints or database migrations (fields already exist).
- Adding these fields to other job-related forms (Job List, Order Record, etc.) — that can be a follow-up if needed.

## Decisions

1. **Place the new fields in the existing form layout group**
   - Decision: Add `soNumber` and `originalSONumber` as readonly text inputs in the same column structure as other reference fields (e.g., near `customerReference` or `purchaseOrder`).
   - Rationale: Keeps the form layout consistent and doesn't introduce new sections.
   - Alternative considered: Adding a separate "Sales Order" section. Rejected as premature — only two fields don't warrant a new section yet.

2. **Mark fields as readonly**
   - Decision: Use `readonly="true"` on the v-text-fields to prevent user editing, matching legacy behavior where these are system-populated values.
   - Rationale: Prevents accidental data entry errors; these values typically come from external sales order systems.

3. **Extend existing TypeScript types additively**
   - Decision: Add `soNumber?: string` and `originalSONumber?: string` to `JobOrderFormData` and `JobDetail` interfaces.
   - Rationale: Optional properties maintain backward compatibility with any code that doesn't yet reference them.

4. **Map through existing service layer**
   - Decision: Include the new fields in the `CreateJobRequest` and `UpdateJobRequest` mappings within `jobs.ts`.
   - Rationale: Reuses the existing save flow without introducing new API endpoints.

## Risks / Trade-offs

- [Form becomes visually crowded] -> Mitigation: Place fields in existing empty slots or collapse them under a subtle label group if needed.
- [Backward compatibility with older API clients] -> Mitigation: Properties are optional, so absence doesn't break anything.
- [i18n key drift across locales] -> Mitigation: Add a checklist task to verify all three locales have the keys before merging.
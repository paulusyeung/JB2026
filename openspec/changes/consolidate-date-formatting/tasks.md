## 1. Core Infrastructure

- [ ] 1.1 Create centralized date formatting utility at `src/utils/dateFormatter.ts`.
- [ ] 1.2 Implement Pinia store at `src/stores/dateFormat.ts` to manage global format state.
- [ ] 1.3 Create Vue composable at `src/composables/useGlobalDateFormatter.ts` for unified access.
- [ ] 1.4 Add unit tests for `dateFormatter.ts` to verify all format types and edge cases (null/invalid dates).

## 2. Component Migration

- [ ] 2.1 Identify and list all components using legacy date formatting (e.g., `OrderListView.vue`, `JobListView.vue`, `StockView.vue`).
- [ ] 2.2 Migrate `OrderListView.vue` to use `useGlobalDateFormatter`.
- [ ] 2.3 Migrate `JobListView.vue` to use `useGlobalDateFormatter`.
- [ ] 2.4 Migrate `StockView.vue` to use `useGlobalDateFormatter`.
- [ ] 2.5 Migrate other identified components (e.g., `SmlInvoiceListView.vue`, `QuotationsView.vue`, `ReportsView.vue`).

## 3. Global Settings Integration

- [ ] 3.1 Create a date format selection UI in the settings view.
- [ ] 3.2 Bind the selection UI to `dateFormatStore.setCurrentFormat`.
- [ ] 3.3 Verify that changing the global format updates all migrated components reactively.

## 4. Cleanup and Optimization

- [ ] 4.1 Remove redundant local formatting functions from components.
- [ ] 4.2 Audit the codebase for any remaining `new Date().toLocaleDateString()` calls that should be centralized.
- [ ] 4.3 Optimize `dateFormatter.ts` by caching `Intl.DateTimeFormat` instances if performance issues are observed.
- [ ] 4.4 Final smoke test across all affected views.

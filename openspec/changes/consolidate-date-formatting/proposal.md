## Why

Date formatting is currently fragmented across the application, with multiple local utility functions (`formatDate`, `formatDateTime`, `formatDateYMD`, `formatDateCell`) defined within various Vue components. This fragmentation makes it difficult to maintain consistency in date presentation, complicates global format changes (e.g., switching from MM/DD/YYYY to YYYY-MM-DD), and leads to code duplication. Consolidating these into a centralized configuration system will improve maintainability and ensure a uniform user experience.

## What Changes

- Create a centralized date formatting utility (`src/utils/dateFormatter.ts`) with predefined format types (short, long, ISO, etc.).
- Implement a global Pinia store (`src/stores/dateFormat.ts`) to manage the current formatting preference.
- Create a Vue composable (`src/composables/useGlobalDateFormatter.ts`) for easy access to formatting functions within components.
- Replace fragmented local formatting functions with the unified global formatter.
- Update UI components to use the centralized system, allowing for dynamic format updates based on user settings or global configuration.

## Implementation Learnings to Preserve

- Use `Intl.DateTimeFormat` for localization support, leveraging the Existing `useLocaleFormatters` logic for locale detection.
- Handle null, undefined, and invalid date strings gracefully by returning a consistent placeholder (e.g., '-').
- Support ISO formats explicitly for data-heavy views or CSV exports where standard ISO strings are required.
- Maintain compatibility with the current `useLocaleFormatters` to avoid breaking existing localization workflows while layering the global format preference on top.

## Capabilities

### New Capabilities
- `centralized-date-formatting`: Provide a unified API and global configuration for date presentation across all application views.

### Modified Capabilities
- None.

## Impact

- **Developer Productivity**: Simplifies date formatting tasks with a single, well-documented utility.
- **Maintainability**: Centralizes formatting logic, making global UI changes easier to implement.
- **Consistency**: Ensures dates look identical across different modules and views.
- **State Management**: Introduces a new Pinia store for UI preferences related to dates.

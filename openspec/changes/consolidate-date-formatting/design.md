## Context

The application currently lacks a centralized system for date formatting, resulting in fragmented implementations across various Vue components. Developers often define local formatting functions (`formatDate`, `formatDateTime`, `formatDateYMD`, etc.) which leads to inconsistency and high maintenance overhead. This design proposes a unified approach using a centralized utility, a global Pinia store for formatting preferences, and a Vue composable for easy consumption.

## Goals / Non-Goals

**Goals:**
- Centralize date formatting logic in a single utility module.
- Provide a global Pinia store to manage user-selected or system-wide date format preferences.
- Implement a Vue composable to provide a reactive and easy-to-use API for components.
- Standardize on `Intl.DateTimeFormat` for robust localization and formatting.
- Ensure graceful handling of null, undefined, and invalid date inputs.

**Non-Goals:**
- Replacing specialized date libraries if they are used for complex date math or relative time (this focus is on *presentation*).
- Redesigning the API date serialization format (this focus is on *UI formatting*).
- Implementing a full-blown i18n system (this will leverage existing `vue-i18n` integration).

## Decisions

1. **Centralized Utility (`src/utils/dateFormatter.ts`)**
   - Rationale: Provides a pure-function baseline for formatting that can be used outside of Vue components (e.g., in services or tests).
   - Key Feature: Defines a `DATE_FORMATS` constant and a `formatDate` function that uses `Intl.DateTimeFormat`.

2. **Pinia Store (`src/stores/dateFormat.ts`)**
   - Rationale: Allows global state management for the preferred date format, which can be hooked into a settings UI.
   - Key Feature: Stores `currentFormat` and provides a `format` method that uses the current state.

3. **Composable (`src/composables/useGlobalDateFormatter.ts`)**
   - Rationale: Simplifies usage in Vue 3 script setup, providing reactive access to the store's state and methods.
   - Key Feature: Exposes a `format` function that defaults to the global setting but allows overrides.

4. **Graceful Degradation**
   - Rationale: User data may contain unexpected nulls or malformed strings.
   - Key Feature: Default to '-' for missing or invalid dates to maintain UI layout stability.

## Risks / Trade-offs

- [Migration Effort] -> Mitigation: Use a phased approach by updating the most critical views first and providing a compatibility wrapper if needed.
- [Performance] -> Mitigation: `Intl.DateTimeFormat` is natively optimized in modern browsers; caching formatter instances in the utility can further improve performance if needed.
- [Breaking Changes] -> Mitigation: Ensure the new `formatDate` signature is flexible enough to accommodate existing call sites with minimal friction.

## Technical Details

### Proposed Core Utility
```typescript
// src/utils/dateFormatter.ts
export const DATE_FORMATS = {
  SHORT_DATE: 'shortDate',
  SHORT_DATETIME: 'shortDateTime',
  SHORT_TIME: 'shortTime',
  LONG_DATE: 'longDate',
  LONG_DATETIME: 'longDateTime',
  CUSTOM: 'custom',
  ISO_DATE: 'isoDate',
  ISO_DATETIME: 'isoDateTime'
} as const;

export type DateFormatType = typeof DATE_FORMATS[keyof typeof DATE_FORMATS];

export function formatDate(
  value: string | Date | null | undefined,
  format: DateFormatType = DATE_FORMATS.SHORT_DATE,
  locale: string = 'en-US'
): string {
  if (!value) return '-';
  const date = typeof value === 'string' ? new Date(value) : value;
  if (isNaN(date.getTime())) return '-';

  if (format === DATE_FORMATS.ISO_DATE) return date.toISOString().split('T')[0];
  if (format === DATE_FORMATS.ISO_DATETIME) return date.toISOString();

  const options: Record<DateFormatType, Intl.DateTimeFormatOptions> = {
    [DATE_FORMATS.SHORT_DATE]: { year: 'numeric', month: '2-digit', day: '2-digit' },
    [DATE_FORMATS.SHORT_DATETIME]: { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' },
    [DATE_FORMATS.SHORT_TIME]: { hour: '2-digit', minute: '2-digit' },
    [DATE_FORMATS.LONG_DATE]: { year: 'numeric', month: 'long', day: 'numeric' },
    [DATE_FORMATS.LONG_DATETIME]: { year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit', second: '2-digit' },
    [DATE_FORMATS.CUSTOM]: { year: 'numeric', month: '2-digit', day: '2-digit' }, // Placeholder for dynamic custom
    [DATE_FORMATS.ISO_DATE]: {}, // Handled above
    [DATE_FORMATS.ISO_DATETIME]: {} // Handled above
  };

  return new Intl.DateTimeFormat(locale, options[format]).format(date);
}
```

## Open Questions

- Should we cache `Intl.DateTimeFormat` instances in a Map to improve performance for high-frequency renders (e.g., large tables)?
- Do we need to support time zone overrides globally, or is local time/UTC sufficient for the initial implementation?

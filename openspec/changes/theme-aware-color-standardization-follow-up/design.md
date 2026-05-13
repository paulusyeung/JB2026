## Context

The initial theme-aware color standardization change successfully refactored the high-impact QuotationsView, admin workflow components, and shared styles. This follow-up applies the same proven pattern to the remaining 16+ list views and dashboard views that still use hardcoded RGBA header colors and chart grid colors. The pattern is now established and well-tested, making this phase a straightforward repetition of the same approach.

## Goals / Non-Goals

**Goals:**
- Apply the same theme-aware Vuetify variable refactoring pattern to all remaining list views with hardcoded header colors.
- Replace SettingsView RGBA gradient backgrounds with Vuetify theme variables.
- Replace DashboardView chart grid colors from hardcoded RGBA to Vuetify theme tokens.
- Ensure all views now consistently respond to Light/Dark theme switches.
- Maintain visual parity with the initial phase to avoid regression.

**Non-Goals:**
- Introduce new design system features or theme colors.
- Redesign layouts or component hierarchy.
- Modify backend APIs or server-rendered business logic.
- Add new user-facing theme selection features.

## Decisions

1. **Apply identical pattern to all 16+ list views.**
   - Decision: Use the same header color variable refactoring from QuotationsView (hardcoded RGBA → `rgb(var(--v-theme-surface-variant))`).
   - Rationale: The initial phase validated this approach. Reusing it reduces review complexity and ensures consistency.
   - Alternative considered: Create new semantic variables for list view headers. Rejected — unnecessary abstraction.

2. **Map SettingsView RGBA gradients to Vuetify surface color gradients.**
   - Decision: Replace hardcoded RGBA gradients with gradients using `rgb(var(--v-theme-surface))` and `rgb(var(--v-theme-surface-variant))`.
   - Rationale: Matches the hero card refactoring pattern from initial phase.
   - Alternative considered: Use primary color gradients. Rejected — would be too visually dominant for settings page.

3. **Update DashboardView chart grid colors to use Vuetify outline token.**
   - Decision: Replace hardcoded RGBA grid overlay colors with `rgba(var(--v-theme-outline), opacity)`.
   - Rationale: Outline token provides appropriate neutral grid lines that adapt to theme.
   - Alternative considered: Use surface-variant. Rejected — outline is more appropriate for fine grid lines.

4. **Batch all 16+ views in a single change for consistency.**
   - Decision: Process all remaining list views together to ensure uniform variable usage.
   - Rationale: Single PR review, reduces fragmentation, easier to validate consistency.
   - Alternative considered: Separate by view type (lists vs. dashboards). Rejected — adds unnecessary process overhead.

## Risks / Trade-offs

- [Risk] Some list views may have custom CSS that expects specific hex values for contrast. → Mitigation: Validate WCAG AA contrast in both themes; adjust to on-surface or on-surface-variant token if needed.
- [Risk] DashboardView chart grid overlay is complex; incorrect opacity values could affect readability. → Mitigation: Test grid visibility against both light and dark backgrounds; use opacity values proven in initial phase.
- [Trade-off] The pattern is now well-established but creates temporary inconsistency between already-refactored and pending views until this phase completes. → Mitigation: Plan for rapid review and merge to minimize window.

## Migration Plan

1. Process all 16+ list views in batch:
   - Identify hardcoded `rgba(195, 216, 248, 0.92)` and `rgba(52, 74, 104, 0.95)` in Vue component styles
   - Replace with `rgb(var(--v-theme-surface-variant))` and `rgb(var(--v-theme-on-surface-variant))`
   - Remove any `*--dark` class toggles in templates
   
2. Refactor SettingsView gradients:
   - Replace hardcoded RGBA backgrounds with Vuetify theme gradients
   
3. Refactor DashboardView chart grid:
   - Replace hardcoded grid RGBA overlays with `rgba(var(--v-theme-outline), ...)`
   
4. Validation:
   - Run application in both Light and Dark themes
   - Verify all list headers update on theme switch
   - Check chart grids remain visible and readable
   - Screenshot comparison with initial phase refactored views for consistency

## Open Questions

- Should we create a shared CSS custom property (e.g., `--list-header-bg`) that maps to Vuetify variables, to reduce duplication across 16 views? Or keep using Vuetify variables directly in each view's scoped styles?
  - **Tentative answer**: Keep direct Vuetify variables for now; simpler and aligns with initial phase pattern.

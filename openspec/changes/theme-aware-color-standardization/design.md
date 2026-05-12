## Context

The frontend currently mixes Vuetify-managed theme variables (`--v-theme-*`) with hardcoded component colors and a second layer of custom `--shell-*` variables defined in `main.scss`. This dual-source color model causes visual inconsistency during Light/Dark switching, especially in table headers and layout surfaces. The proposed change standardizes color usage around Vuetify CSS theme variables so all visible surfaces and text update from a single theme source.

## Goals / Non-Goals

**Goals:**
- Standardize color usage in affected views and shared styles to use Vuetify theme CSS variables.
- Remove hardcoded fallback RGB tuples that can keep stale colors after theme changes.
- Ensure key application areas remain legible and visually consistent in Light and Dark themes.
- Reduce custom per-view dark-mode override classes in favor of token-driven styling.

**Non-Goals:**
- Introduce a new design system beyond current Vuetify theme primitives.
- Redesign layout structure, spacing, or typography.
- Change backend APIs or server-rendered business behavior.
- Add new user-facing theme selection features.

## Decisions

1. **Use Vuetify theme variables as the single source of truth.**
- Decision: Replace hardcoded hex, rgba values, and custom `--shell-*` variables with Vuetify theme-aware values such as `rgb(var(--v-theme-surface-variant))` and `rgb(var(--v-theme-on-surface-variant))`.
- Rationale: Vuetify already coordinates theme state. The existing `main.scss` defines a second color layer (`--shell-ink`, `--shell-paper`, `--shell-accent`, etc.) with manual dark-theme overrides via `[data-theme='dark']`, creating a dual-source model that drifts over time. Consolidating to `--v-theme-*` eliminates this maintenance burden.
- Alternative considered: Keep the `--shell-*` layer and map it to Vuetify variables. Rejected — the shell layer duplicates Vuetify's built-in surface/surface-variant/on-surface tokens without adding any unique semantic meaning.

2. **Prioritize high-impact files first, then broaden by audit.**
- Decision: Begin with `QuotationsView.vue`, `main.scss` shell variables, and `_Layout.cshtml.css`, then remove stale RGB fallback tuples in admin workflow components. Defer the 11+ remaining views with the shared dark-header RGBA pattern to a follow-up change.
- Rationale: Delivers visible consistency quickly and limits regression risk through staged scope.
- Alternative considered: Full style rewrite in one pass. Rejected — increases review complexity and rollback difficulty.

3. **Keep changes behaviorally scoped to color adaptation.**
- Decision: Avoid non-color style refactors during this change.
- Rationale: Narrow scope makes parity checks straightforward and supports safe incremental rollout.

4. **Treat `_Layout.cshtml.css` SSRed styles as client-only concern.**
- Decision: `_Layout.cshtml.css` contains fully hardcoded Bootstrap hex colors (e.g. `#1b6ec2`, `#e5e5e5`). These styles only affect the brief SSR hydration window before Vuetify takes over and are visually overridden by the SPA shell once mounted. Update to theme-aware values if feasible via `<html>`-level CSS custom properties; otherwise accept that SSR flash is out of scope and defer to a later pass.
- Rationale: Vuetify `--v-theme-*` variables are scoped to `.v-application`, which does not exist during SSR. Attempting to inject them server-side would require non-trivial middleware changes.

## Risks / Trade-offs

- [Risk] Some legacy styles may rely on fixed contrast assumptions. -> Mitigation: Validate WCAG AA contrast (≥4.5:1 normal text, ≥3:1 large text) in both themes on prioritized views before expansion.
- [Risk] `--v-theme-*` variables are scoped under `.v-application`, not `:root`. Styles outside the Vuetify component tree (e.g. `_Layout.cshtml.css`) won't inherit them. -> Mitigation: Accept SSR flash as out of scope or use matching `--shell-*` fallbacks only where the Vuetify scope doesn't reach.
- [Risk] Deep selectors in Vuetify components can be brittle across library updates. -> Mitigation: Keep selectors minimal, avoid `!important`, and colocate rationale in review notes.
- [Risk] Removing fallback tuples from expressions like `rgb(var(--v-theme-surface, 245, 245, 245))` may expose unset variable usage in rare contexts. -> Mitigation: Validate key routes under both themes and adjust to a supported Vuetify token where needed.
- [Trade-off] Incremental migration leaves temporary mixed patterns outside audited files. -> Mitigation: Track remaining 11+ views as a follow-up change and enforce `--v-theme-*`-only guidance in PR reviews.
- [Trade-off] Replacing the `--shell-*` layer with `--v-theme-*` means losing the custom warm-toned palette (e.g. `#f5f4ee` paper, `#9f4f2a` accent). -> Mitigation: If the custom palette is desired, define it in Vuetify's theme config itself rather than a parallel CSS variable layer.

## Context

ClientApp already has a centralized route title model:

- Routes define `meta: { titleKey: '...' }` in `router/index.ts`.
- Document title is resolved from the route title key and localized through i18n.

However, `AppTopbar.vue` still renders static labels (`topbar.phase`, `topbar.workspace`) and individual views render duplicated in-content title/subtitle blocks. This produces two competing title systems.

Current route records are effectively flat (no nested `children` records), so route title selection is unambiguous today.

## Goals / Non-Goals

**Goals:**
- Show app name `JB2026` as the topbar eyebrow label.
- Show the active page name dynamically in topbar, derived from route metadata.
- Remove duplicated page title/subtitle banner blocks from route-backed authenticated views.
- Preserve localization behavior for dynamic page names.

**Non-Goals:**
- Redesigning the full visual style of all pages.
- Changing route paths, route names, or access control.
- Changing login/auth screen layout that does not render inside app shell.

## Decisions

1. **Route metadata remains the source of truth for page names**
   - Topbar title should resolve from current route title key, using the same i18n resolution approach and fallback semantics already used for document title.
   - This avoids introducing parallel per-view title constants.
   - Canonical label namespace is `routes.*`; feature-local `*.title` keys are not used for topbar identity.

2. **App name is fixed at shell level**
   - Topbar eyebrow should display `common.appName` (rendering as `JB2026`).

3. **Remove page banners only, not all headings**
   - For authenticated route-backed views, remove duplicate top-of-page banner blocks only.
   - Definition: a page banner is the leading intro title/subtitle block at the top of the view's main card that repeats route identity.
   - Non-page section titles (for example dashboard chart headings) and functional toolbar headers must remain.

4. **Use explicit page classification before removals**
   - Classify candidate views as:
     - A: safe full banner removal,
     - B: remove only banner sub-block while preserving controls in the same header row,
     - C: no change (section heading/tool header, not duplicate page banner).
   - Apply changes only to A/B classes.

5. **Keep spacing stable after heading removal**
   - Where heading removal affects vertical rhythm, adjust card title/body spacing minimally to preserve readability and prevent toolbar overlap.

## Risks / Trade-offs

- **Coverage risk across many views**: Missing one route view would result in inconsistent UX.
  - *Mitigation*: Use a grep-driven checklist for all view files containing top-level heading/subtitle intro patterns.

- **Over-removal risk**: Treating any `h3` subtitle pair as removable would delete legitimate section headers.
  - *Mitigation*: enforce A/B/C page classification and page-banner definition before edits.

- **I18n key cleanup risk**: Removing subtitle blocks may leave unused translation keys.
  - *Mitigation*: defer aggressive translation cleanup to a follow-up pass unless keys are clearly unreferenced.

- **Title mismatch risk**: Some pages currently use local heading keys different from route title keys.
  - *Mitigation*: treat route metadata (`routes.*`) labels as canonical and align any outliers during implementation.

## Migration Plan

Frontend-only, no backend migration.

1. Update topbar dynamic title behavior using existing router title semantics and fallback.
2. Inventory and classify views (A/B/C).
3. Remove duplicate page banners for A/B only.
4. Run lint/tests and manual route sweep for visual regressions.

## Open Questions

- Should `LegacyMenuPlaceholderView.vue` keep explanatory copy after local page banner removal, or move all route identity to topbar only?

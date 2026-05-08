## Why

The current app shell duplicates page context in two places:

- `AppTopbar.vue` shows static copy (`topbar.phase` and `topbar.workspace`) that does not reflect the active route.
- Many route-backed views repeat a local title/subtitle block at the top of each page (for example `Job List` plus descriptive subtitle), creating visual redundancy and inconsistent hierarchy.

The desired UX is a single authoritative page header in the topbar:

- Top line should always show the app name (`JB2026`).
- Main line should show the currently loaded page name.
- View-level duplicate title/subtitle banners should be removed across ClientApp pages that render inside the authenticated shell.

## What Changes

- **Replace** the static topbar eyebrow text source so it resolves to the app name (`JB2026`) rather than `Phase 6`.
- **Make topbar page title dynamic** by binding the second line to the active route's resolved title (via the route record `meta: { titleKey: '...' }` and i18n translation).
- **Mirror existing router fallback semantics** so when a route has no title key, topbar title falls back to `common.appName`.
- **Remove duplicated page title/subtitle blocks** from route-backed views rendered under the authenticated app shell.
- **Scope banner removal precisely** to top-of-page duplicate page banners only; keep section headings and tool headers inside content cards.
- **Preserve route metadata as the single source of truth** for page names, so topbar title updates correctly on navigation and locale changes.
- **Keep auth layout behavior unchanged** (for example, login screen remains outside app shell and does not require topbar title behavior).

## Capabilities

### New Capabilities
- `topbar-route-title-context`: App topbar displays app identity + current route page title in real time.

### Modified Capabilities
- `clientapp-view-chrome`: Route-backed views no longer render duplicate local title/subtitle banners when the topbar already provides page context.

## Impact

- **Affected shell files**:
  - `JB2026.WebApp/ClientApp/src/components/layout/AppTopbar.vue`
  - `JB2026.WebApp/ClientApp/src/App.vue` (if title context is passed from shell)
  - `JB2026.WebApp/ClientApp/src/router/index.ts` (existing `meta.titleKey` remains the title source)
- **Affected views**:
  - Route-backed views in `JB2026.WebApp/ClientApp/src/views/` that currently render top-level `h3` title + subtitle intro blocks (for example Job List, Order List, Stock, Reports, Admin pages, Settings, SML, schedules).
- **i18n impact**:
  - Route `routes.*` translation keys are canonical for topbar and document titles.
  - Keep or deprecate legacy per-view subtitle keys based on residual usage after cleanup.

## Validation Focus

- Verify topbar first line consistently shows `JB2026`.
- Verify topbar second line changes with each route navigation and matches route title labels.
- Verify locale switch updates the dynamic topbar page title without refresh.
- Verify fallback title is `common.appName` when a route does not provide a title key.
- Verify duplicate per-view title/subtitle blocks are removed from authenticated shell pages.
- Verify section headers (for example dashboard chart titles) and functional tool headers are not removed.
- Verify core page layouts still render correctly after heading block removal (spacing, first actionable controls, and responsive behavior).

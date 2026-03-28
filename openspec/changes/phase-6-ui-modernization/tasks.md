# Tasks — phase-6-ui-modernization

## Group 1: Vue 3 SPA Foundation

- [x] Scaffold Vue 3 + Vite project inside the JB2026 solution
- [x] Configure Vue Router 4 with lazy-loaded route modules
- [x] Configure Pinia store layout (one store per domain module)
- [x] Integrate Vuetify 3 with a base theme matching legacy brand colours
- [x] Configure axios (or fetch wrapper) for authenticated API calls
- [x] Set up Playwright project and write first smoke test against the Vue dev server
- [x] Add ESLint with `vue/component-api-style` rule set to `script-setup` only

## Group 2: DevExpress Replacement — Grids and Forms

- [x] Confirm `devexpress-replacement-spike` findings (from Phase 1) and document gap list
- [x] Replace legacy grid views with `v-data-table-server` including sort, filter, pagination
- [x] Replace DevExpress form controls with Vuetify text fields, selects, and date pickers
- [x] Implement virtual-scroll composable for grids exceeding 500 rows
- [x] Remove all DevExpress NPM packages and confirm CI licence scan passes

## Group 3: DevExpress Replacement — Scheduler and Charts

- [x] Integrate FullCalendar (Apache 2.0) with resource and timeline views
- [x] Implement drag-and-drop event update → API call
- [x] Integrate Chart.js (MIT) for dashboard charts and reports
- [x] Remove DevExpress scheduler and chart assemblies/packages

## Group 4: CKEditor 5 OSS Migration

- [x] Install `@ckeditor/ckeditor5-build-classic` (GPL v2 open-source build)
- [x] Configure toolbar: bold, italic, lists, tables, links (minimum set)
- [x] Validate legacy CKEditor 4 HTML content renders correctly in CKEditor 5
- [x] Remove proprietary CKEditor 4 packages; confirm CI licence scan passes
- [ ] Obtain legal sign-off on GPL v2 licence for open-source release

## Group 5: UI Slice Migration (per slice: A → D)

- [ ] **Slice A** — Read-only list/dashboard views: build, Playwright smoke, UAT sign-off, flag flip
- [ ] **Slice B** — Create/edit form views: build, Playwright smoke, UAT sign-off, flag flip
- [ ] **Slice C** — Scheduler/calendar views: build, Playwright smoke, UAT sign-off, flag flip
- [ ] **Slice D** — Rich-text editor views: build, Playwright smoke, UAT sign-off, flag flip

## Group 6: Feature Flag Routing

- [x] Implement feature flag store (SQL table or config file)
- [x] Implement flag-aware routing middleware in ASP.NET Core (60-second TTL cache)
- [x] Write integration tests: disabled flag → legacy route; enabled flag → SPA route
- [x] Document flag toggle runbook for operations team

## Group 7: UAT and Smoke-Test Gate

- [ ] Conduct UAT round with product owner; capture acceptance artefacts per slice
- [ ] Confirm all Playwright smoke suites pass in staging

## Group 8: Phase 6 Quality Gate

- [ ] Zero DevExpress runtime references in production bundle (CI scan)
- [ ] Zero proprietary CKEditor packages in production bundle (CI scan)
- [ ] All Playwright smoke suites green in CI
- [ ] UAT acceptance artefact on file for every migrated slice
- [ ] All feature flags flipped to enabled in staging; legacy WebForms routes still reachable via override

# ClientApp Menu Parity Audit

Date: 2026-03-30

## Executive Totals

Using the strict acceptance rule in this document:

- Total visible ClientApp sidebar items: 11
- Clearly ported modern screens: 11
- Shell-only or coexistence items: 0
- Strict ported percentage across all visible sidebar items: 100.0%

Legacy-parity composition of the 11 visible items:

- Exact legacy top-level menu matches: 4
- Legacy-derived but reshaped items: 5
- New JB2026 items not validated as top-level legacy menu entries: 2

Legacy-origin coverage under the same strict rule:

- Legacy-origin or legacy-derived menu concepts represented in ClientApp: 9
- Legacy-origin or legacy-derived concepts fully ported as dedicated modern screens: 9
- Strict ported percentage across legacy-origin concepts only: 100.0%

Ported items counted in the strict total:

- `Dashboard`
- `Jobs`
- `Quotations`
- `Rich Text`
- `Scheduler`
- `Job Order`
- `Stock`
- `Admin`
- `Settings`
- `Reports`
- `SML`

Shell-only visible items counted in the strict total:


Hidden planned routes not counted in the visible sidebar total:

- `Public`
- `Help`

Hidden-but-implemented routes:

- `Public`
- `Help`

## Scope

This audit validates the current JB2026 ClientApp visible sidebar menu against the legacy JB2015 Job.Book navigation definitions and identifies which entries are actually ported versus represented as coexistence shells. It also notes hidden planned routes that remain defined in the router but are no longer shown in the sidebar.

Primary evidence sources:

- JB2026 sidebar: `JB2026.WebApp/ClientApp/src/components/layout/AppSidebar.vue`
- JB2026 routes: `JB2026.WebApp/ClientApp/src/router/index.ts`
- JB2026 legacy slice shell: `JB2026.WebApp/ClientApp/src/views/LegacySliceView.vue`
- JB2026 legacy slice dependency registry: `JB2026.WebApp/Controllers/LegacySlicesController.cs`
- JB2015 role menus:
  - `C:/Projects/JB2015/Job.Book/Resources/Menu/NavMenu4Admin.xml`
  - `C:/Projects/JB2015/Job.Book/Resources/Menu/NavMenu4Manager.xml`
  - `C:/Projects/JB2015/Job.Book/Resources/Menu/NavMenu4Supervisor.xml`
  - `C:/Projects/JB2015/Job.Book/Resources/Menu/NavMenu4Operator.xml`
  - `C:/Projects/JB2015/Job.Book/Resources/Menu/NavMenu4Guest.xml`

## Validation Rules

- `Ported` means the menu item routes to a dedicated modern Vue screen with concrete behavior implemented in ClientApp.
- `Shell only` means the menu item exists in ClientApp, but it routes to the generic legacy slice control-plane page rather than a migrated feature screen.
- `Exact legacy menu match` means the menu label exists in the legacy role-menu XML as a navigable category.
- `Legacy-derived but reshaped` means the concept exists in legacy navigation, but the JB2026 menu changed its navigation level or presentation.
- `New item` means the entry is present in JB2026 but was not found as a top-level legacy role-menu category in the inspected XML.

## Sidebar Inventory

### Modern section

| ClientApp item | Current route | Status | Legacy parity | Notes |
| --- | --- | --- | --- | --- |
| Dashboard | `/dashboard` | Ported | New item | New modernization shell and KPI overview, not found in inspected legacy role-menu XML. |
| Jobs | `/jobs` | Ported | Legacy-derived but reshaped | Legacy navigation exposed `Order List`, `Job List`, and `Job Stats` under `Job Order`; JB2026 consolidates this into a modern Jobs screen. |
| Quotations | `/quotations` | Ported | Legacy-derived but reshaped | Legacy admin menu exposed `Quotation List` under `Job Order`; JB2026 promotes quotations to a dedicated modern route. |
| Rich Text | `/editor` | Ported | New item | New modernization slice for editor replacement, not found as a legacy role-menu category. |
| Scheduler | `/scheduler` | Ported | Legacy-derived but reshaped | Legacy exposed `Job Schedule` under `Job Order`; JB2026 promotes it to its own modern route. |

### Legacy Modules section

| ClientApp item | Current route | Status | Legacy parity | Notes |
| --- | --- | --- | --- | --- |
| Job Order | `/job-order` | Ported | Exact legacy menu match | Exists as `<joborder Caption="Job Order">` in legacy menus and now routes to a dedicated modern Job Order register backed by `/api/v2/job-orders`. |
| SML | `/sml` | Ported | Legacy-derived but reshaped | Legacy has `SML` nested under `Job Order`, not as a top-level category. JB2026 exposes it as a top-level sidebar item backed by `GET /api/v2/sml/stats`. |
| Stock | `/stock` | Ported | Exact legacy menu match | Exists as `<stock Caption="Stock">` in legacy menus and now routes to a dedicated modern Stock view backed by `GET /api/v2/stock/products`. |
| Reports | `/reports` | Ported | Legacy-derived but reshaped | Legacy has `Reports` nested under `Job Order`, not as a top-level category. JB2026 exposes it as a top-level sidebar item backed by `POST /api/v2/reports/run`. |
| Admin | `/admin` | Ported | Exact legacy menu match | Exists as `<admin Caption="Admin">` in legacy menus and now routes to a dedicated Admin user directory backed by `/api/v2/admin/users`. |
| Settings | `/settings` | Ported | Exact legacy menu match | Exists as `<settings Caption="Settings">` in legacy menus and now routes to a dedicated Settings view backed by `/api/v2/settings`. |

### Hidden planned routes

| Route item | Current route | Sidebar visibility | Legacy parity | Notes |
| --- | --- | --- | --- | --- |
| Public | `/public` | Hidden | New item | Route remains hidden from the sidebar by information architecture choice and now routes to a dedicated Public view backed by `GET /api/v2/public/content`. |
| Help | `/help` | Hidden | New item | Route remains hidden from the sidebar by information architecture choice and now routes to a dedicated Help view backed by `GET /api/v2/help/articles`. |

## Evidence Matrix

The table below adds direct evidence for each ClientApp sidebar item.

| Item | JB2026 sidebar evidence | JB2026 route evidence | Port type evidence | Legacy evidence |
| --- | --- | --- | --- | --- |
| Dashboard | `AppSidebar.vue:44` | `router/index.ts:12` | Dedicated Vue route | Not found in inspected `NavMenu4*.xml` top-level categories |
| Jobs | `AppSidebar.vue:45` | `router/index.ts:18` | Dedicated Vue route and API usage in `services/jobs.ts:10` | Legacy capabilities appear as `Order List`, `Job List`, `Job Stats` under `NavMenu4Admin.xml:5-7` |
| Quotations | `AppSidebar.vue:46` | `router/index.ts:24` | Dedicated Vue route and API usage in `services/quotations.ts:10` | Legacy `Quotation List` appears in `NavMenu4Admin.xml:4` |
| Rich Text | `AppSidebar.vue:47` | `router/index.ts:36` | Dedicated Vue route | Not found in inspected `NavMenu4*.xml` top-level categories |
| Scheduler | `AppSidebar.vue:48` | `router/index.ts:42` | Dedicated Vue route and API usage in `services/scheduler.ts:10` | Legacy `Job Schedule` appears nested under `Job Order` in `NavMenu4Admin.xml:8` |
| Job Order | `AppSidebar.vue:52` | `router/index.ts:48` | Dedicated Vue route and API usage in `services/jobOrders.ts` | Top-level `<joborder Caption="Job Order">` in `NavMenu4Admin.xml:3` |
| SML | `AppSidebar.vue:53` | `router/index.ts:55` | Dedicated Vue route and API usage in `services/sml.ts` | Nested under Job Order in `NavMenu4Admin.xml:15` |
| Stock | `AppSidebar.vue:63` | `router/index.ts:62` | Dedicated Vue route and API usage in `services/stock.ts` | Top-level `<stock Caption="Stock">` in `NavMenu4Admin.xml:25` |
| Reports | `AppSidebar.vue:55` | `router/index.ts:69` | Dedicated Vue route and API usage in `services/reports.ts` | Nested under Job Order in `NavMenu4Admin.xml:21` |
| Admin | `AppSidebar.vue:56` | `router/index.ts:76` | Dedicated Vue route and API usage in `services/admin.ts` | Top-level `<admin Caption="Admin">` in `NavMenu4Admin.xml:28` |
| Settings | `AppSidebar.vue:55` | `router/index.ts:90` | Dedicated Vue route and API usage in `services/settings.ts` | Top-level `<settings Caption="Settings">` in `NavMenu4Admin.xml:41` |

Hidden route evidence:

- Public route remains defined in `router/index.ts:83`
- Help route remains defined in `router/index.ts:97`

## Shell Detection Rule

The following evidence is used to classify a sidebar item as `Shell only`:

- Its route imports `LegacySliceView.vue` in `JB2026.WebApp/ClientApp/src/router/index.ts`.
- `LegacySliceView.vue` renders readiness metadata and representative legacy routes rather than a migrated business screen.
- The screen includes legacy entry-point links and computed routing status instead of feature-specific UI controls.

Concrete evidence:

- `LegacySliceView.vue:8` describes the page as a module mapped from a Job.Book folder for migration planning.
- `LegacySliceView.vue:35` describes the listed URLs as representative Job.Book routes.
- `LegacySliceView.vue:51` renders direct links to legacy route paths.

## Pending Backend Coverage Evidence

The server-side registry now marks `Public` and `Help` API dependencies as implemented.

## Implemented Modern Coverage Evidence

The following entries have concrete modern UI and backend or HTTP integration evidence:

- Job Order:
  - sidebar entry `AppSidebar.vue:52`
  - dedicated route `router/index.ts:48`
  - API usage `services/jobOrders.ts`
- Admin:
  - sidebar entry `AppSidebar.vue:56`
  - dedicated route `router/index.ts:76`
  - API usage `services/admin.ts`
- Settings:
  - sidebar entry `AppSidebar.vue:55`
  - dedicated route `router/index.ts:90`
  - API usage `services/settings.ts`
- Public:
  - hidden route `router/index.ts:78`
  - dedicated route `router/index.ts:78`
  - API usage `services/publicContent.ts`
- Help:
  - hidden route `router/index.ts:91`
  - dedicated route `router/index.ts:91`
  - API usage `services/help.ts`
- Jobs:
  - sidebar entry `AppSidebar.vue:45`
  - dedicated route `router/index.ts:18`
  - API usage `services/jobs.ts:10,18,28,30`
- Quotations:
  - sidebar entry `AppSidebar.vue:46`
  - dedicated route `router/index.ts:24`
  - API usage `services/quotations.ts:10,18`
- Scheduler:
  - sidebar entry `AppSidebar.vue:48`
  - dedicated route `router/index.ts:42`
  - API usage `services/scheduler.ts:10,21`
- SML:
  - sidebar entry `AppSidebar.vue:53`
  - dedicated route `router/index.ts:55`
  - API usage `services/sml.ts`
- Stock:
  - sidebar entry `AppSidebar.vue:63`
  - dedicated route `router/index.ts:62`
  - API usage `services/stock.ts`
- Reports:
  - sidebar entry `AppSidebar.vue:55`
  - dedicated route `router/index.ts:69`
  - API usage `services/reports.ts`
- Authentication and profile foundation used by the SPA:
  - `services/auth.ts:5,14`

## Legacy Menu Findings

### Confirmed top-level legacy categories

The inspected legacy role-menu XML consistently shows these top-level categories where applicable:

- `Job Order`
- `Admin`
- `Settings`

### Confirmed legacy nested categories later promoted in JB2026

The legacy menus also show these categories nested inside `Job Order` rather than promoted to the top level:

- `SML`
- `Reports`
- `Quotation List`
- `Job Schedule`

This means the JB2026 sidebar is not doing a strict one-to-one reproduction of legacy navigation. It is normalizing legacy capabilities into clearer top-level SPA routes.

### Items not validated as legacy top-level navigation

The following JB2026 sidebar items were not found as top-level categories in the inspected legacy role-menu XML:

- `Dashboard`
- `Rich Text`

The hidden routes `Public` and `Help` are also not validated as top-level categories in the inspected legacy role-menu XML.

These may still correspond to legacy folders or flows, but they are not validated as top-level role-menu entries from the XML navigation source.

## Port Status by Menu Item

### Clearly ported modern screens

These items route to dedicated Vue views rather than the generic legacy slice shell:

- `Dashboard`
- `Jobs`
- `Quotations`
- `Rich Text`
- `Scheduler`
- `Job Order`
- `SML`
- `Stock`
- `Reports`
- `Admin`
- `Settings`

Current interpretation: these are the only sidebar items that should be counted as ported UI elements today.

### Not yet ported as full screens

All visible sidebar items now route to dedicated Vue views instead of `LegacySliceView.vue`.

The `Public` and `Help` routes remain hidden from the visible sidebar for navigation scope reasons, but both now have implemented backing contracts and dedicated modern screens.

## API and Dependency Readiness

The legacy slice registry in `JB2026.WebApp/Controllers/LegacySlicesController.cs` indicates a mixed state:

- Implemented or mapped dependencies are recorded for job-order, quotations, auth, and current-profile flows.
- No API dependencies in the current legacy-slice registry are marked pending for the migrated modules.

This means some sidebar entries have backend coverage, but that does not by itself mean the ClientApp menu entry is ported. A menu item should only be considered ported when it routes to a dedicated migrated screen instead of the generic legacy slice shell.

## Conclusions

1. The JB2026 ClientApp sidebar now has full visible-route port coverage, while still applying modernization reshaping relative to legacy menu hierarchy.
2. The top-level legacy categories `Job Order`, `Stock`, `Admin`, and `Settings` are represented.
3. `SML` and `Reports` are legacy-derived, but JB2026 promotes them from nested legacy entries to top-level sidebar items.
4. `Dashboard`, `Rich Text`, `Public`, and `Help` are not validated as top-level legacy role-menu entries from the inspected XML.
5. All eleven visible sidebar items are clearly ported as actual modern screens today:
   - `Dashboard`
   - `Jobs`
   - `Quotations`
   - `Rich Text`
   - `Scheduler`
  - `Job Order`
  - `SML`
  - `Stock`
  - `Reports`
  - `Admin`
  - `Settings`
6. The non-visible `Public` and `Help` routes are now also implemented and remain hidden by sidebar design choice.

## Recommended Acceptance Rule

For future reporting, count a ClientApp menu item as `ported` only if all of the following are true:

- It routes to a dedicated Vue view, not `LegacySliceView.vue`.
- The screen renders functional business UI, not just readiness metadata.
- Required API or HTTP dependencies for that screen are implemented.
- The route can remain inside the SPA without falling back to a legacy placeholder or redirect for core workflows.
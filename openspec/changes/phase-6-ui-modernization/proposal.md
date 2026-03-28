# Proposal — phase-6-ui-modernization

## Why

The legacy JB2015 front end is built on WebForms controls and proprietary DevExpress ASP.NET components. These cannot run on .NET 8 and are incompatible with open-source redistribution. Phase 6 replaces the entire UI layer with a Vue 3 single-page application that consumes the ASP.NET Core APIs delivered in Phase 4.

## What Changes

The legacy WebForms `.aspx` pages and code-behind files are replaced with Vue 3 components. DevExpress UI controls are replaced with Vuetify 3 (data tables, dialogs, date pickers) and open-source alternatives. The rich-text editor is migrated from the proprietary CKEditor licence to CKEditor 5 open-source build. All UI routing moves to Vue Router 4 and state management moves to Pinia.

## Capabilities

- `vue3-component-migration` — systematic conversion of each WebForms view to a Vue 3 SFC, routed via Vue Router, backed by Pinia stores
- `devexpress-oss-replacement` — all DevExpress grid, scheduler, and chart components replaced with Vuetify 3, FullCalendar (MIT), and Chart.js
- `ckeditor5-oss-migration` — proprietary CKEditor licence replaced with CKEditor 5 open-source (GPL v2/MIT dual-licence) build
- `ui-feature-flag-routing` — feature-flag coexistence layer so legacy WebForms pages remain accessible until each Vue 3 slice is fully validated and flipped
- `ui-slice-uat-and-smoke-gates` — UAT sign-off process and Playwright end-to-end test suite gate for each migrated UI slice

## Impact

- **Users** — No change in workflow; UI appearance evolves progressively per slice
- **Backend** — No changes; APIs were stabilised in Phase 3
- **Operations** — Static Vue 3 build is served from the same web host via a base-href prefix until full cutover
- **Redistribution** — All runtime UI dependencies are MIT or GPL-compatible; DevExpress and proprietary CKEditor licences are removed

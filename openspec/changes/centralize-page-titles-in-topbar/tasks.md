## 1. Topbar Dynamic Context

- [x] 1.1 Update `AppTopbar.vue` eyebrow line to show `t('common.appName')` (rendering as `JB2026`) instead of `topbar.phase`
- [x] 1.2 Bind topbar main title line to current route title key from route metadata (`meta: { titleKey: '...' }`) plus i18n
- [x] 1.3 Mirror router fallback semantics: when title key is missing, topbar title falls back to `common.appName`
- [x] 1.4 Ensure title reacts to both route changes and locale changes without refresh
- [x] 1.5 Keep existing navigation/theme/language/profile/sign-out controls unchanged

## 2. Classify and Remove Duplicate Page Banners

**Reference: [IMPLEMENTATION_INVENTORY.md](IMPLEMENTATION_INVENTORY.md)** for concrete A/B/C file classification.

- [x] 2.1 Review the A/B/C inventory in IMPLEMENTATION_INVENTORY.md
- [x] 2.2 Remove duplicate page banners for class A views (18 views, safe full removal)
- [x] 2.3 Remove only title/subtitle sub-block for class B views (6 views, preserve controls/layout)
- [x] 2.4 Verify no changes to class C views (7 views: dashboard charts, job detail panels, toolbars, etc.)
- [x] 2.5 Preserve layout spacing and alignment after removal (no toolbar collision or cramped content)
- [x] 2.6 Verify no functional controls were accidentally removed with banner edits

## 3. Localization and Metadata Consistency

- [x] 3.1 Confirm each affected route has a valid title key in route metadata
- [x] 3.2 Ensure topbar dynamic title uses the same localized label source as document title
- [x] 3.3 Treat `routes.*` keys as canonical for route identity labels
- [x] 3.4 Resolve any route/view naming mismatches uncovered during sweep

## 4. Validation

- [x] 4.1 Manual check: topbar shows `JB2026` and correct page name across major routes
- [x] 4.2 Manual check: topbar fallback is `common.appName` when route title key is missing
- [x] 4.3 Manual check: page banner duplication is gone for class A/B views
- [x] 4.4 Manual check: class C section/tool headers remain intact
- [x] 4.5 Manual check: desktop and mobile layouts still render correctly after banner removal
- [x] 4.6 Run lint/tests relevant to ClientApp and resolve regressions introduced by this change

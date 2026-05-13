## 1. Component Refactoring

- [x] 1.1 Replace hardcoded header and surface colors in Quotations view with Vuetify theme CSS variables.
- [x] 1.2 Remove custom dark-mode class toggles tied to component-level hardcoded color variables.
- [x] 1.3 Verify targeted view styling still matches expected hierarchy and readability after token migration.

## 2. Shared Style Standardization

- [x] 2.1 Audit main stylesheet shell and utility color variables for hardcoded hex and rgba values.
- [x] 2.2 Map shared shell and layout background or border colors to theme-aware Vuetify variables.
- [x] 2.3 Update server-rendered layout stylesheet backgrounds to resolve from active theme values.

## 3. Fallback Cleanup

- [x] 3.1 Remove stale hardcoded RGB tuple fallbacks from theme-dependent color expressions in targeted admin workflow styles.
- [x] 3.2 Confirm updated expressions still resolve valid colors in both Light and Dark themes.

## 4. Validation and Regression

- [x] 4.1 Execute visual regression checks for representative routes in Light and Dark themes.
- [x] 4.2 Verify no unreadable text or mismatched surfaces remain on audited screens.
- [x] 4.3 Record remaining non-compliant files as follow-up tasks if discovered during validation.

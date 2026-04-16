## 1. Theme Definition
- [x] 1.1 Update `src/main.ts` to define the 6 new color schemes as Vuetify themes.
- [x] 1.2 Rename existing `jb2026Light` and `jb2026Dark` to `light-nature` and `dark-forest` for consistency.
- [x] 1.3 Verify that all themes have appropriate contrast for background, surface, and primary colors.

## 2. Store and State Management
- [x] 2.1 Update `src/stores/theme.ts` to include `scheme` in the store state.
- [x] 2.2 Re-implement `readStoredTheme` and `setTheme` to handle the new state structure (mode + scheme).
- [x] 2.3 Implement migration logic in `readStoredTheme` to convert legacy string values ('light'/'dark') to the new format.
- [x] 2.4 Add a `setScheme` method to the theme store.

## 3. Application Hookup
- [x] 3.1 Update `src/App.vue` to compute the `v-app` theme name dynamically based on both mode and scheme.
- [x] 3.2 Update the `watch` in `App.vue` to correctly set `document.documentElement` attributes for CSS targeting.

## 4. UI Implementation
- [x] 4.1 Create a new settings component (e.g., `ThemeSettings.vue`) for scheme selection.
- [x] 4.2 Integrate the theme settings into a relevant view (e.g., Topbar or a Settings page).
- [x] 4.3 Add visual indicators for the currently selected scheme.

## 5. Verification and Polish
- [x] 5.1 Test theme switching in both directions (Mode toggle and Scheme selection).
- [x] 5.2 Verify persistence across page refreshes and browser restarts.
- [x] 5.3 Conduct a broad visual sweep of the application to ensure color schemes apply correctly to all component types.

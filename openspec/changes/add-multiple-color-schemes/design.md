## Context

The current application uses a hardcoded binary theme selection (Light/Dark) in `main.ts` and `App.vue`. The user preference is managed by a Pinia store (`src/stores/theme.ts`) that persists the choice to `localStorage` under the key `jb2026.theme`. This design expands the system to support multiple color palettes (schemes) within each mode.

## Goals / Non-Goals

**Goals:**
- Define 3 refined color schemes for Light mode and 3 for Dark mode.
- Update `main.ts` to register 6 distinct Vuetify themes.
- Modify `useThemeStore` to manage and persist both the mode and the specific scheme.
- Ensure the UI reactively updates when a new scheme is selected.
- Maintain backward compatibility (current theme choice maps to the new default schemes).

**Non-Goals:**
- Creating a dynamic theme generator (allowing users to pick arbitrary hex codes).
- Changing the layout or structure of the components (this is a color-only update).
- Implementing system-level accent color detection (e.g., Windows/macOS accent colors).

## Decisions

1. **Named Themes in Vuetify (`src/main.ts`)**
   - Rationale: Vuetify supports multiple named themes. Registering them all at startup makes switching instantaneous.
   - naming: `light-nature`, `light-indigo`, `light-rose`, `dark-forest`, `dark-midnight`, `dark-amethyst`.

2. **Expanded Pinia State (`src/stores/theme.ts`)**
   - Rationale: We need to track the "active scheme" separately from "is dark".
   - State: `mode: 'light' | 'dark'` and `scheme: string`.
   - Persistence: Update the `localStorage` value to a JSON string or use a new key to avoid conflicts with simple string values from the previous version.

3. **Theme Component Mapping (`src/App.vue`)**
   - Rationale: `v-app` needs a single theme name.
   - Mapping: A computed property will combine `mode` and `scheme` to determine the Vuetify theme ID (e.g., `themeStore.mode + '-' + themeStore.scheme`).

4. **Curated Palettes**
   - Rationale: Professional consistency is better than random colors.
   - Palettes:
     - **Nature (Default Light)**: Cream/Brown base.
     - **Indigo (Light)**: High-contrast blue/white.
     - **Rose (Light)**: Warm pink/grey.
     - **Forest (Default Dark)**: Dark green/brown base.
     - **Midnight (Dark)**: Navy/Black/Cyan.
     - **Amethyst (Dark)**: Purple/Dark Grey/Lavender.

## Risks / Trade-offs

- **Storage Format Breakage**: Changing the `localStorage` format from a plain string ('light'/'dark') to an object/JSON could cause issues for existing users.
  - *Mitigation*: The `readStoredTheme` function will check if the stored value is an old-style string and migration it gracefully to the new format.
- **Component Specific Styles**: Some components might have hardcoded colors (e.g., `color="primary"`) that don't look good in all schemes.
  - *Mitigation*: Review key views to ensure they use Vuetify's semantic color names.

## Technical Details

### Proposed Theme Definitions (Draft)

```typescript
// Proposed colors for Indigo (Light)
{
  dark: false,
  colors: {
    background: '#f8fafc',
    surface: '#ffffff',
    primary: '#1e40af', // Blue 800
    secondary: '#0ea5e9', // Sky 500
    accent: '#f59e0b', // Amber 500
  }
}

// Proposed colors for Midnight (Dark)
{
  dark: true,
  colors: {
    background: '#020617', // Slate 950
    surface: '#0f172a', // Slate 900
    primary: '#38bdf8', // Sky 400
    secondary: '#1e293b', // Slate 800
    accent: '#818cf8', // Indigo 400
  }
}
```

### Store Expansion

```typescript
export const useThemeStore = defineStore('theme', () => {
  const mode = ref<'light'|'dark'>(...) 
  const scheme = ref<string>('default')

  const currentVuetifyTheme = computed(() => `${mode.value}-${scheme.value}`)
  
  // setter will save { mode, scheme } to localStorage
})
```

## Open Questions

- Should we provide a visual preview in the settings UI? (Recommended: Yes, later).
- Do we want to support an "Auto" mode that follows the system OS mode but uses a specific scheme? (Recommended: Yes).

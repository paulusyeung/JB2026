## Why

The application currently only supports a binary Light and Dark mode with fixed color palettes. While functional, modern user interfaces often provide several color schemes to better suit different user preferences, accessibility needs (like higher contrast), and aesthetic tastes. Adding multiple modern color schemes will make the application feel more premium, customizable, and visually engaging.

## What Changes

- **Multiple Schemes per Mode**: Introduce 3 distinct color schemes for Light mode (Nature, Indigo, Rose) and 3 for Dark mode (Forest, Midnight, Amethyst).
- **Persistent Scheme Preference**: Update the theme store and persistence logic to remember not just the mode (Light/Dark) but also the specific color scheme selected by the user.
- **Vuetify Theme Expansion**: Define 6 distinct themes in the Vuetify configuration, allowing for seamless switching between color palettes.
- **Themed UI Elements**: Ensure that primary, secondary, and accent colors are applied consistently across all components using Vuetify's theme system.

## Implementation Learnings to Preserve

- **Existing Colors**: Keep the current `jb2026Light` and `jb2026Dark` as the "Nature" and "Forest" (default) schemes to maintain continuity for existing users.
- **Vuetify Theme Properties**: Continue using Vuetify's built-in theme properties (`primary`, `secondary`, `accent`, `background`, `surface`) rather than hardcoded CSS colors.
- **LocalStorage Persistence**: Leverage the existing `localStorage` based persistence system in `useThemeStore`.

## Capabilities

### New Capabilities
- `multiple-color-schemes`: Users can choose from a library of modern color palettes for both light and dark modes.
- `theme-scheme-persistence`: The application remembers the specific color scheme preference across sessions.

### Modified Capabilities
- `global-theme-management`: The `useThemeStore` is expanded to manage both mode and scheme state simultaneously.

## Impact

- **User Experience**: Provides a personalized feel and allows users to choose palettes that are easiest on their eyes or simply more pleasing.
- **Aesthetics**: Elevates the app's visual design with professionally curated, modern color palettes.
- **Accessibility**: Offers potential for higher-contrast schemes in the future by establishing the selection framework now.
- **Modernity**: Aligns the application with modern UI trends seen in major IDEs and productivity tools.

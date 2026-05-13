import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { themeRegistry } from '@/themes/registry'

const THEME_STORAGE_KEY = 'jb2026.theme.v2'
const LEGACY_THEME_STORAGE_KEY = 'jb2026.theme'

export const appModes = ['light', 'dark'] as const
export type AppMode = (typeof appModes)[number]

// Derived from registry: all light schemes and all dark schemes
export const appSchemes = {
  light: themeRegistry
    .filter(pair => pair.light.dark === false)
    .map(pair => pair.id) as readonly string[],
  dark: themeRegistry
    .filter(pair => pair.dark.dark === true)
    .map(pair => pair.id) as readonly string[],
} as const

export type AppScheme = string

interface ThemeState {
  mode: AppMode
  scheme: string
}

export const useThemeStore = defineStore('theme', () => {
  const state = ref<ThemeState>(readStoredTheme())

  const mode = computed(() => state.value.mode)
  const scheme = computed(() => state.value.scheme)
  const isDark = computed(() => state.value.mode === 'dark')
  const vuetifyTheme = computed(() => `${state.value.mode}-${state.value.scheme}`)

  function setMode(nextMode: AppMode) {
    state.value.mode = nextMode
    // Ensure scheme is valid for new mode, if not, reset to default
    if (!(appSchemes[nextMode] as readonly string[]).includes(state.value.scheme)) {
      state.value.scheme = nextMode === 'light' ? 'nature' : 'forest'
    }
    saveTheme()
  }

  function setScheme(nextScheme: string) {
    state.value.scheme = nextScheme
    saveTheme()
  }

  function toggleTheme() {
    setMode(isDark.value ? 'light' : 'dark')
  }

  function saveTheme() {
    localStorage.setItem(THEME_STORAGE_KEY, JSON.stringify(state.value))
  }

  return {
    mode,
    scheme,
    isDark,
    vuetifyTheme,
    setMode,
    setScheme,
    toggleTheme,
  }
})

function readStoredTheme(): ThemeState {
  // 1. Try new storage format
  const stored = localStorage.getItem(THEME_STORAGE_KEY)
  if (stored) {
    try {
      const parsed = JSON.parse(stored) as ThemeState
      if (appModes.includes(parsed.mode)) {
        return parsed
      }
    } catch (e) {
      console.warn('Failed to parse theme settings', e)
    }
  }

  // 2. Try legacy migration
  const legacy = localStorage.getItem(LEGACY_THEME_STORAGE_KEY)
  if (legacy === 'light' || legacy === 'dark') {
    return {
      mode: legacy,
      scheme: legacy === 'light' ? 'nature' : 'forest',
    }
  }

  // 3. Fallback to system preference
  const systemMode =
    typeof window !== 'undefined' && window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'

  return {
    mode: systemMode,
    scheme: systemMode === 'light' ? 'nature' : 'forest',
  }
}
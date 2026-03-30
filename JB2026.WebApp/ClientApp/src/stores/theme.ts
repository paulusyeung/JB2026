import { computed, ref } from 'vue'
import { defineStore } from 'pinia'

const THEME_STORAGE_KEY = 'jb2026.theme'

export const appThemes = ['light', 'dark'] as const

export type AppTheme = (typeof appThemes)[number]

export const useThemeStore = defineStore('theme', () => {
  const current = ref<AppTheme>(readStoredTheme())

  const isDark = computed(() => current.value === 'dark')

  function setTheme(nextTheme: AppTheme) {
    current.value = nextTheme
    localStorage.setItem(THEME_STORAGE_KEY, nextTheme)
  }

  function toggleTheme() {
    setTheme(isDark.value ? 'light' : 'dark')
  }

  return {
    current,
    isDark,
    setTheme,
    toggleTheme,
  }
})

function readStoredTheme(): AppTheme {
  const storedTheme = localStorage.getItem(THEME_STORAGE_KEY)

  if (storedTheme === 'light' || storedTheme === 'dark') {
    return storedTheme
  }

  return 'light'
}
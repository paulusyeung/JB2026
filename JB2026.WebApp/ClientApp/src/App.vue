<template>
  <v-app :theme="vuetifyThemeName">
    <v-layout class="app-shell">
      <AppSidebar v-model="mobileNavOpen" :is-mobile="isMobile" :is-collapsed="desktopSidebarCollapsed" />
      <v-main>
        <div class="app-frame">
          <AppTopbar
            :is-mobile="isMobile"
            :is-sidebar-collapsed="desktopSidebarCollapsed"
            @toggle-navigation="handleNavigationToggle"
          />
          <router-view />
        </div>
      </v-main>
    </v-layout>
  </v-app>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useDisplay, useTheme } from 'vuetify'
import AppSidebar from '@/components/layout/AppSidebar.vue'
import AppTopbar from '@/components/layout/AppTopbar.vue'
import { getSettings } from '@/services/settings'
import { useDateFormatStore } from '@/stores/dateFormat'
import { useSessionStore } from '@/stores/session'
import { useThemeStore } from '@/stores/theme'
import { DATE_FORMATS, type DateFormatType } from '@/utils/dateFormatter'

const themeStore = useThemeStore()
const sessionStore = useSessionStore()
const dateFormatStore = useDateFormatStore()
const theme = useTheme()
const display = useDisplay()
const mobileNavOpen = ref(false)
const desktopSidebarCollapsed = ref(false)
const syncingDateFormat = ref(false)

const vuetifyThemeName = computed(() => themeStore.vuetifyTheme)
const isMobile = computed(() => display.mdAndDown.value)
const knownDateFormats = new Set<DateFormatType>(Object.values(DATE_FORMATS))

watch(isMobile, (mobile) => {
  if (!mobile) {
    mobileNavOpen.value = false
  }
})

watch(
  () => sessionStore.isAuthenticated,
  (isAuthenticated) => {
    if (!isAuthenticated) {
      return
    }

    void syncDateFormatPreference()
  },
  { immediate: true },
)

watch(
  () => themeStore.vuetifyTheme,
  (themeName) => {
    // Vuetify 3.4+ recommendation: use theme.global.name.value but the warning specifically 
    // asks to use theme.name if using the theme object correctly from useTheme()
    theme.global.name.value = themeName
    document.documentElement.dataset.theme = themeStore.mode
    document.documentElement.dataset.scheme = themeStore.scheme
    document.documentElement.style.colorScheme = themeStore.mode
  },
  { immediate: true },
)

function handleNavigationToggle() {
  if (isMobile.value) {
    mobileNavOpen.value = !mobileNavOpen.value
    return
  }

  desktopSidebarCollapsed.value = !desktopSidebarCollapsed.value
}

async function syncDateFormatPreference() {
  if (!sessionStore.isAuthenticated || syncingDateFormat.value) {
    return
  }

  syncingDateFormat.value = true

  try {
    const settings = await getSettings()
    const nextFormat = settings.dateFormatPreference as DateFormatType
    if (knownDateFormats.has(nextFormat)) {
      dateFormatStore.setCurrentFormat(nextFormat)
    }
  } catch {
    // Keep the current in-memory format when settings cannot be loaded.
  } finally {
    syncingDateFormat.value = false
  }
}
</script>
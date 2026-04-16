<template>
  <v-app :theme="vuetifyThemeName">
    <v-layout class="app-shell">
      <AppSidebar />
      <v-main>
        <div class="app-frame">
          <AppTopbar />
          <router-view />
        </div>
      </v-main>
    </v-layout>
  </v-app>
</template>

<script setup lang="ts">
import { computed, watch } from 'vue'
import { useTheme } from 'vuetify'
import AppSidebar from '@/components/layout/AppSidebar.vue'
import AppTopbar from '@/components/layout/AppTopbar.vue'
import ThemeSettings from '@/components/settings/ThemeSettings.vue'
import { useThemeStore } from '@/stores/theme'

const themeStore = useThemeStore()
const theme = useTheme()

const vuetifyThemeName = computed(() => themeStore.vuetifyTheme)

watch(
  () => themeStore.vuetifyTheme,
  (themeName) => {
    theme.global.name.value = themeName
    document.documentElement.dataset.theme = themeStore.mode
    document.documentElement.dataset.scheme = themeStore.scheme
    document.documentElement.style.colorScheme = themeStore.mode
  },
  { immediate: true },
)
</script>
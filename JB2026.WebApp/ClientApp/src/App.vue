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
import { useThemeStore } from '@/stores/theme'

const themeStore = useThemeStore()
const theme = useTheme()

const vuetifyThemeName = computed(() => (themeStore.current === 'dark' ? 'jb2026Dark' : 'jb2026Light'))

watch(
  () => themeStore.current,
  (currentTheme) => {
    theme.global.name.value = currentTheme === 'dark' ? 'jb2026Dark' : 'jb2026Light'
    document.documentElement.dataset.theme = currentTheme
    document.documentElement.style.colorScheme = currentTheme
  },
  { immediate: true },
)
</script>
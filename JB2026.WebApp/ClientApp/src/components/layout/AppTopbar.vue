<template>
  <header class="topbar">
    <div>
      <p class="eyebrow mb-1">{{ t('topbar.phase') }}</p>
      <h2 class="text-h5 mb-0">{{ t('topbar.workspace') }}</h2>
    </div>

    <div class="topbar-actions">
      <v-menu :close-on-content-click="false" location="bottom end">
        <template v-slot:activator="{ props }">
          <v-btn
            icon
            v-bind="props"
            variant="outlined"
            color="primary"
            class="mr-2"
          >
            <v-icon :icon="themeStore.isDark ? 'mdi-weather-night' : 'mdi-white-balance-sunny'" />
          </v-btn>
        </template>
        <v-card min-width="300" class="pa-4">
          <ThemeSettings />
        </v-card>
      </v-menu>
      <v-select
        :model-value="selectedLocale"
        :items="localeOptions"
        item-title="label"
        item-value="value"
        :label="t('topbar.language')"
        density="compact"
        variant="outlined"
        hide-details
        style="max-width: 180px"
        @update:model-value="handleLocaleChange"
      />
      <div class="text-right" v-if="session.profile">
        <div class="text-subtitle-2">{{ session.profile.displayName }}</div>
        <div class="text-caption text-medium-emphasis">{{ session.profile.role }}</div>
      </div>
      <v-btn v-if="session.isAuthenticated" variant="outlined" color="primary" @click="handleLogout">
        {{ t('topbar.signOut') }}
      </v-btn>
      <v-chip color="secondary" variant="flat">{{ t('topbar.spaHost') }}</v-chip>
    </div>
  </header>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useSessionStore } from '@/stores/session'
import { localeOptions, type AppLocale } from '@/i18n/messages'
import { setLocale } from '@/i18n'
import { useThemeStore } from '@/stores/theme'
import ThemeSettings from '@/components/settings/ThemeSettings.vue'

const router = useRouter()
const session = useSessionStore()
const themeStore = useThemeStore()
const { t, locale } = useI18n({ useScope: 'global' })

const selectedLocale = computed(() => locale.value as AppLocale)

function handleLocaleChange(nextLocale: AppLocale | null) {
  if (!nextLocale) {
    return
  }

  setLocale(nextLocale)
}

function handleLogout() {
  session.logout()
  router.push({ name: 'login' })
}
</script>
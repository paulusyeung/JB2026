<template>
  <header class="topbar">
    <div class="topbar-summary">
      <v-btn
        icon
        variant="outlined"
        color="primary"
        class="topbar-nav-btn"
        :aria-label="isMobile ? t('topbar.navigation') : sidebarToggleLabel"
        @click="$emit('toggle-navigation')"
      >
        <v-icon :icon="isMobile ? 'mdi-menu' : isSidebarCollapsed ? 'mdi-chevron-double-right' : 'mdi-chevron-double-left'" />
      </v-btn>

      <div>
        <p class="eyebrow mb-1">{{ t('topbar.phase') }}</p>
        <h2 class="text-h5 mb-0">{{ t('topbar.workspace') }}</h2>
      </div>
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

      <template v-if="!isMobile">
        <v-select
          :model-value="selectedLocale"
          :items="localeOptions"
          item-title="label"
          item-value="value"
          :label="t('topbar.language')"
          density="compact"
          variant="outlined"
          hide-details
          class="topbar-locale"
          @update:model-value="handleLocaleChange"
        />
        <div class="text-right topbar-identity" v-if="session.profile">
          <div class="text-subtitle-2">{{ session.profile.displayName }}</div>
          <div class="text-caption text-medium-emphasis">{{ session.profile.role }}</div>
        </div>
        <v-btn v-if="session.isAuthenticated" variant="outlined" color="primary" @click="handleLogout">
          {{ t('topbar.signOut') }}
        </v-btn>
        <v-chip color="secondary" variant="flat">{{ t('topbar.spaHost') }}</v-chip>
      </template>

      <v-menu v-else location="bottom end">
        <template #activator="{ props }">
          <v-btn
            icon
            v-bind="props"
            variant="outlined"
            color="primary"
            :aria-label="t('topbar.moreActions')"
          >
            <v-icon icon="mdi-dots-vertical" />
          </v-btn>
        </template>

        <v-card min-width="280" class="pa-3 topbar-menu-panel">
          <v-select
            :model-value="selectedLocale"
            :items="localeOptions"
            item-title="label"
            item-value="value"
            :label="t('topbar.language')"
            density="compact"
            variant="outlined"
            hide-details
            class="mb-3"
            @update:model-value="handleLocaleChange"
          />

          <div class="topbar-identity mb-3" v-if="session.profile">
            <div class="text-subtitle-2">{{ session.profile.displayName }}</div>
            <div class="text-caption text-medium-emphasis">{{ session.profile.role }}</div>
          </div>

          <v-btn v-if="session.isAuthenticated" block variant="outlined" color="primary" class="mb-3" @click="handleLogout">
            {{ t('topbar.signOut') }}
          </v-btn>

          <v-chip color="secondary" variant="flat">{{ t('topbar.spaHost') }}</v-chip>
        </v-card>
      </v-menu>
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

const props = defineProps<{
  isMobile: boolean
  isSidebarCollapsed: boolean
}>()

defineEmits<{
  'toggle-navigation': []
}>()

const router = useRouter()
const session = useSessionStore()
const themeStore = useThemeStore()
const { t, locale } = useI18n({ useScope: 'global' })

const selectedLocale = computed(() => locale.value as AppLocale)
const sidebarToggleLabel = computed(() =>
  props.isSidebarCollapsed ? t('topbar.expandNavigation') : t('topbar.collapseNavigation'),
)

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
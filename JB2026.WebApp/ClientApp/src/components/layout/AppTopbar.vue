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
        <p class="eyebrow mb-1">{{ t('common.appName') }}</p>
        <h2 class="text-h5 mb-0">{{ pageTitle }}</h2>
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
        <v-card width="480" min-width="0" class="pa-4">
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
        <div class="text-right topbar-identity" v-if="session.profile" @click="openProfileEditor">
          <div class="text-subtitle-2">{{ session.profile.displayName }}</div>
          <div class="text-caption text-medium-emphasis">{{ session.profile.role }}</div>
        </div>
        <v-btn
          v-if="session.isAuthenticated"
          icon
          variant="outlined"
          color="primary"
          :aria-label="t('topbar.signOut')"
          @click="handleLogout"
        >
          <v-icon icon="mdi-logout" />
        </v-btn>
      </template>

      <v-menu v-else v-model="mobileMenuOpen" location="bottom end">
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

          <div class="topbar-identity mb-3" v-if="session.profile" @click="openProfileEditor">
            <div class="text-subtitle-2">{{ session.profile.displayName }}</div>
            <div class="text-caption text-medium-emphasis">{{ session.profile.role }}</div>
          </div>

          <v-btn
            v-if="session.isAuthenticated"
            block
            variant="outlined"
            color="primary"
            class="mb-3"
            @click="handleLogout"
          >
            <v-icon class="mr-2" icon="mdi-logout" />
            {{ t('topbar.signOut') }}
          </v-btn>
        </v-card>
      </v-menu>
    </div>

    <v-dialog v-model="recordDialogOpen" max-width="min(100%, 760px)" scrollable>
      <StaffMemberRecordDialog
        :user-id="editingUserId"
        @saved="handleRecordSaved"
        @cancel="recordDialogOpen = false"
      />
    </v-dialog>
  </header>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useSessionStore } from '@/stores/session'
import { localeOptions, type AppLocale } from '@/i18n/messages'
import { setLocale } from '@/i18n'
import { useThemeStore } from '@/stores/theme'
import ThemeSettings from '@/components/settings/ThemeSettings.vue'
import StaffMemberRecordDialog from '@/components/crm/StaffMemberRecordDialog.vue'

const props = defineProps<{
  isMobile: boolean
  isSidebarCollapsed: boolean
}>()

defineEmits<{
  'toggle-navigation': []
}>()

const router = useRouter()
const route = useRoute()
const session = useSessionStore()
const themeStore = useThemeStore()
const { t, locale } = useI18n({ useScope: 'global' })

const pageTitle = computed(() => {
  const titleKey = typeof route.meta.titleKey === 'string' ? route.meta.titleKey : undefined
  return titleKey ? t(titleKey) : t('common.appName')
})

const mobileMenuOpen = ref(false)
const recordDialogOpen = ref(false)
const editingUserId = ref<string | null>(null)

function openProfileEditor() {
  mobileMenuOpen.value = false
  if (!session.profile) return
  editingUserId.value = session.profile.userId
  recordDialogOpen.value = true
}

async function handleRecordSaved() {
  await session.bootstrapProfile()
}

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

<style scoped>
.topbar-identity {
  cursor: pointer;
}
</style>

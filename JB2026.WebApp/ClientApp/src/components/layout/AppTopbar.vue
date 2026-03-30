<template>
  <header class="topbar">
    <div>
      <p class="eyebrow mb-1">{{ t('topbar.phase') }}</p>
      <h2 class="text-h5 mb-0">{{ t('topbar.workspace') }}</h2>
    </div>

    <div class="topbar-actions">
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
      <v-btn v-if="session.isAuthenticated" variant="outlined" color="primary" @click="session.logout()">
        {{ t('topbar.signOut') }}
      </v-btn>
      <v-chip color="secondary" variant="flat">{{ t('topbar.spaHost') }}</v-chip>
    </div>
  </header>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSessionStore } from '@/stores/session'
import { localeOptions, type AppLocale } from '@/i18n/messages'
import { setLocale } from '@/i18n'

const session = useSessionStore()
const { t, locale } = useI18n({ useScope: 'global' })

const selectedLocale = computed(() => locale.value as AppLocale)

function handleLocaleChange(nextLocale: AppLocale | null) {
  if (!nextLocale) {
    return
  }

  setLocale(nextLocale)
}
</script>
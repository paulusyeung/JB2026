<template>
  <v-navigation-drawer permanent rail-width="82" width="240" class="app-sidebar">
    <div class="brand-lockup">
      <div class="brand-mark">JB</div>
      <div>
        <p class="eyebrow mb-1">{{ t('sidebar.eyebrow') }}</p>
        <h1 class="brand-name">JB2026</h1>
      </div>
    </div>

    <v-list nav density="comfortable" prepend-gap="8">
      <v-list-item
        v-for="item in items"
        :key="item.to"
        :prepend-icon="item.icon"
        :title="item.title"
        :to="item.to"
        rounded="xl"
      />

      <v-list-subheader class="mt-2">{{ t('sidebar.legacyCoreModules') }}</v-list-subheader>
      <MenuItemRenderer :items="legacyMenuItems" />
    </v-list>

    <template #append>
      <div class="sidebar-card">
        <p class="eyebrow mb-2">{{ t('sidebar.coexistence') }}</p>
        <p class="mb-2">{{ t('sidebar.coexistenceBody') }}</p>
        <v-btn block variant="tonal" color="primary" href="/">{{ t('sidebar.controlPlane') }}</v-btn>
      </div>
    </template>
  </v-navigation-drawer>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import MenuItemRenderer from './MenuItemRenderer.vue'
import { buildLegacyMenuItems } from './menuHelper'

const { t } = useI18n({ useScope: 'global' })

const items = computed(() => [
  { title: t('routes.dashboard'), to: '/dashboard', icon: 'mdi-view-dashboard-outline' },
  { title: t('routes.jobs'), to: '/jobs', icon: 'mdi-briefcase-outline' },
  { title: t('routes.quotations'), to: '/quotations', icon: 'mdi-file-document-multiple-outline' },
  { title: t('routes.editor'), to: '/editor', icon: 'mdi-text-box-edit-outline' },
  { title: t('routes.scheduler'), to: '/scheduler', icon: 'mdi-calendar-clock-outline' },
])

const legacyMenuItems = computed(() => buildLegacyMenuItems(t))
</script>
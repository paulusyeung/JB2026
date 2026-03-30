<template>
  <v-navigation-drawer permanent rail-width="82" width="290" class="app-sidebar">
    <div class="brand-lockup">
      <div class="brand-mark">JB</div>
      <div>
        <p class="eyebrow mb-1">{{ t('sidebar.eyebrow') }}</p>
        <h1 class="brand-name">JB2026</h1>
      </div>
    </div>

    <v-list nav density="comfortable">
      <v-list-item
        v-for="item in items"
        :key="item.to"
        :prepend-icon="item.icon"
        :title="item.title"
        :to="item.to"
        rounded="xl"
      />

      <v-list-subheader class="mt-2">{{ t('sidebar.legacyCoreModules') }}</v-list-subheader>
      <v-list-item
        v-for="item in legacyCoreItems"
        :key="item.to"
        :prepend-icon="item.icon"
        :title="item.title"
        :to="item.to"
        rounded="xl"
      />

      <v-list-subheader>{{ t('sidebar.legacyDerivedAreas') }}</v-list-subheader>
      <v-list-item
        v-for="item in legacyDerivedItems"
        :key="item.to"
        :prepend-icon="item.icon"
        :title="item.title"
        :to="item.to"
        rounded="xl"
      />
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

const { t } = useI18n({ useScope: 'global' })

const items = computed(() => [
  { title: t('routes.dashboard'), to: '/dashboard', icon: 'mdi-view-dashboard-outline' },
  { title: t('routes.jobs'), to: '/jobs', icon: 'mdi-briefcase-outline' },
  { title: t('routes.quotations'), to: '/quotations', icon: 'mdi-file-document-multiple-outline' },
  { title: t('routes.editor'), to: '/editor', icon: 'mdi-text-box-edit-outline' },
  { title: t('routes.scheduler'), to: '/scheduler', icon: 'mdi-calendar-clock-outline' },
])

const legacyCoreItems = computed(() => [
  { title: t('routes.jobOrder'), to: '/job-order', icon: 'mdi-clipboard-text-outline' },
  { title: t('routes.stock'), to: '/stock', icon: 'mdi-package-variant-closed' },
  { title: t('routes.admin'), to: '/admin', icon: 'mdi-shield-account-outline' },
  { title: t('routes.settings'), to: '/settings', icon: 'mdi-cog-outline' },
])

const legacyDerivedItems = computed(() => [
  { title: t('routes.sml'), to: '/sml', icon: 'mdi-folder-multiple-outline' },
  { title: t('routes.reports'), to: '/reports', icon: 'mdi-chart-box-outline' },
])
</script>
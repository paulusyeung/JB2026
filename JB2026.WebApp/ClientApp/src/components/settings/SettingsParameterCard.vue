<template>
  <v-card
    rounded="xl"
    elevation="0"
    class="panel-card h-100 settings-parameter-card"
  >
    <v-card-title class="d-flex flex-wrap align-center ga-2">
      <v-icon
        :icon="icon"
        size="small"
        class="text-medium-emphasis"
      />
      <span class="text-subtitle-1">{{ title }}</span>
      <v-spacer />
      <v-chip
        :color="configured ? 'success' : 'warning'"
        variant="tonal"
        size="small"
      >
        {{ configured ? t('systemMonitor.configured') : t('systemMonitor.notConfigured') }}
      </v-chip>
    </v-card-title>

    <v-card-text>
      <v-alert
        v-if="errorMessage"
        type="warning"
        variant="tonal"
        class="mb-2"
      >
        {{ errorMessage }}
      </v-alert>

      <v-skeleton-loader
        v-else-if="loading"
        type="list-item-two-line@3"
        class="mx-n2"
      />

      <v-list
        v-else
        density="compact"
        lines="one"
      >
        <v-list-item
          v-for="row in rows"
          :key="row.label"
          :title="row.value"
          :subtitle="row.label"
          class="px-0"
        />
      </v-list>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'

export interface SettingsParameterRow {
  label: string
  value: string
}

withDefaults(defineProps<{
  title: string
  icon: string
  configured: boolean
  loading?: boolean
  errorMessage?: string
  rows: SettingsParameterRow[]
}>(), {
  loading: false,
  errorMessage: '',
})

const { t } = useI18n({ useScope: 'global' })
</script>
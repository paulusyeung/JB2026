<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title>
        <h3 class="text-h6 mb-1">{{ pageTitle }}</h3>
        <p class="text-body-2 text-medium-emphasis mb-0">{{ t('sidebar.legacyMenuPlaceholder') }}</p>
      </v-card-title>
      <v-card-text>
        <p class="text-body-2 mb-0">{{ route.path }}</p>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'

const route = useRoute()
const { t } = useI18n({ useScope: 'global' })

const pageTitle = computed(() => {
  const key = typeof route.meta.titleKey === 'string' ? route.meta.titleKey : 'routes.dashboard'
  return t(key)
})
</script>

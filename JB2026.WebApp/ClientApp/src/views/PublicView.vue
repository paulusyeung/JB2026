<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('publicContent.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('publicContent.subtitle') }}</p>
        </div>
        <v-spacer />
        <v-btn color="primary" :loading="loading" @click="load">{{ t('common.refresh') }}</v-btn>
      </v-card-title>
      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>

        <v-data-table :headers="headers" :items="rows" :loading="loading" item-value="slug" />
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { getPublicContent } from '@/services/publicContent'
import type { PublicContentItem } from '@/types/api'

const loading = ref(false)
const errorMessage = ref('')
const rows = ref<PublicContentItem[]>([])
const { t } = useI18n({ useScope: 'global' })

const headers = computed(() => [
  { title: t('publicContent.headers.slug'), key: 'slug' },
  { title: t('publicContent.headers.title'), key: 'title' },
  { title: t('publicContent.headers.summary'), key: 'summary' },
  { title: t('publicContent.headers.path'), key: 'urlPath' },
])

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    rows.value = await getPublicContent()
  } catch {
    errorMessage.value = t('publicContent.loadFailed')
  } finally {
    loading.value = false
  }
}
</script>
<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">Public content</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">Published pages and public-facing content catalog.</p>
        </div>
        <v-spacer />
        <v-btn color="primary" :loading="loading" @click="load">Refresh</v-btn>
      </v-card-title>
      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>

        <v-data-table :headers="headers" :items="rows" :loading="loading" item-value="slug" />
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getPublicContent } from '@/services/publicContent'
import type { PublicContentItem } from '@/types/api'

const loading = ref(false)
const errorMessage = ref('')
const rows = ref<PublicContentItem[]>([])

const headers = [
  { title: 'Slug', key: 'slug' },
  { title: 'Title', key: 'title' },
  { title: 'Summary', key: 'summary' },
  { title: 'Path', key: 'urlPath' },
]

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    rows.value = await getPublicContent()
  } catch {
    errorMessage.value = 'Unable to load public content. Please verify API availability.'
  } finally {
    loading.value = false
  }
}
</script>
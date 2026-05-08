<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="public-toolbar d-flex flex-wrap align-center ga-3">
        <v-btn color="primary" :loading="loading" class="public-toolbar__refresh" @click="load">{{ t('common.refresh') }}</v-btn>
      </v-card-title>
      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>

        <ListMobileCard
          v-if="isPhoneLayout"
          :items="rows"
          :columns="mobileColumns"
          item-key="slug"
          :on-card-click="() => undefined"
        />

        <v-data-table v-else :headers="headers" :items="rows" :loading="loading" item-value="slug" />
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import ListMobileCard, { type ListMobileCardColumn } from '@/components/grids/ListMobileCard.vue'
import { useResponsiveList } from '@/composables/useResponsiveList'
import { getPublicContent } from '@/services/publicContent'
import type { PublicContentItem } from '@/types/api'

const loading = ref(false)
const errorMessage = ref('')
const rows = ref<PublicContentItem[]>([])
const { t } = useI18n({ useScope: 'global' })
const { isPhoneLayout } = useResponsiveList()

const headers = computed(() => [
  { title: t('publicContent.headers.slug'), key: 'slug' },
  { title: t('publicContent.headers.title'), key: 'title' },
  { title: t('publicContent.headers.summary'), key: 'summary' },
  { title: t('publicContent.headers.path'), key: 'urlPath' },
])

const mobileColumns = computed<ListMobileCardColumn<PublicContentItem>[]>(() => [
  { key: 'title', label: t('publicContent.headers.title'), section: 'header', emphasis: true },
  { key: 'slug', label: t('publicContent.headers.slug'), section: 'header' },
  { key: 'summary', label: t('publicContent.headers.summary'), section: 'body' },
  { key: 'urlPath', label: t('publicContent.headers.path'), section: 'footer' },
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

<style scoped>
@media (max-width: 960px) {
  .public-toolbar__refresh {
    width: 100%;
  }
}
</style>
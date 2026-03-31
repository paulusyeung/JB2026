<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('quotations.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('quotations.subtitle') }}</p>
        </div>
        <v-spacer />
        <v-text-field
          v-model="store.keyword"
          density="comfortable"
          :label="t('quotations.search')"
          prepend-inner-icon="mdi-magnify"
          variant="solo-filled"
          hide-details
        />
        <v-btn color="primary" @click="store.search">{{ t('common.search') }}</v-btn>
      </v-card-title>
      <v-card-text>
        <v-data-table-server
          v-model:page="store.page"
          v-model:items-per-page="store.itemsPerPage"
          v-model:sort-by="store.sortBy"
          :headers="headers"
          :items="store.rows"
          :items-length="store.rowCount"
          :loading="store.loading"
          item-value="headerId"
        >
          <template #[`item.quotedOn`]="{ item }">
            {{ formatDate(item.quotedOn) }}
          </template>
          <template #[`item.totalCostA`]="{ item }">
            {{ formatMoney(item.totalCostA) }}
          </template>
          <template #[`item.unitCostA`]="{ item }">
            {{ formatMoney(item.unitCostA) }}
          </template>
          <template #[`item.status`]="{ item }">
            <v-chip size="small" color="accent" variant="tonal">{{ t('quotations.status', { value: item.status }) }}</v-chip>
          </template>
        </v-data-table-server>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { useQuotationsStore } from '@/stores/quotations'

const store = useQuotationsStore()
const { t } = useI18n({ useScope: 'global' })
const { formatDate: formatDateByLocale, formatCurrency } = useLocaleFormatters()

const headers = computed(() => [
  { title: t('quotations.headers.quote'), key: 'quoteNumberIndexPair' },
  { title: t('quotations.headers.customer'), key: 'customerName' },
  { title: t('quotations.headers.title'), key: 'printTitle' },
  { title: t('quotations.headers.quotedOn'), key: 'quotedOn' },
  { title: t('quotations.headers.quotedBy'), key: 'quotedBy' },
  { title: t('quotations.headers.total'), key: 'totalCostA' },
  { title: t('quotations.headers.unit'), key: 'unitCostA' },
  { title: t('quotations.headers.status'), key: 'status' },
])

onMounted(async () => {
  if (store.rows.length === 0) {
    await store.load()
  }
})

function formatDate(value: string) {
  return formatDateByLocale(value)
}

function formatMoney(value: number) {
  return formatCurrency(value)
}
</script>
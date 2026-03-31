<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('stock.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('stock.subtitle') }}</p>
        </div>
        <v-spacer />
        <v-text-field
          v-model="keyword"
          density="comfortable"
          :label="t('stock.searchProducts')"
          prepend-inner-icon="mdi-magnify"
          variant="solo-filled"
          hide-details
        />
        <v-btn color="primary" @click="load">{{ t('stock.search') }}</v-btn>
      </v-card-title>
      <v-card-text>
        <v-data-table
          :headers="headers"
          :items="rows"
          :loading="loading"
          item-value="productId"
        >
          <template #[`item.sellingPrice`]="{ item }">
            {{ formatMoney(item.sellingPrice) }}
          </template>
          <template #[`item.cogs`]="{ item }">
            {{ formatMoney(item.cogs) }}
          </template>
          <template #[`item.balance`]="{ item }">
            <v-chip size="small" color="secondary" variant="tonal">{{ item.balance }}</v-chip>
          </template>
        </v-data-table>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { getStockProducts } from '@/services/stock'
import type { StockProductListItem } from '@/types/api'

const rows = ref<StockProductListItem[]>([])
const loading = ref(false)
const keyword = ref('')
const { t } = useI18n({ useScope: 'global' })
const { formatCurrency } = useLocaleFormatters()

const headers = computed(() => [
  { title: t('stock.headers.stockNumber'), key: 'stockNumber' },
  { title: t('stock.headers.code'), key: 'productCode' },
  { title: t('stock.headers.product'), key: 'productName' },
  { title: t('stock.headers.balance'), key: 'balance' },
  { title: t('stock.headers.sellingPrice'), key: 'sellingPrice' },
  { title: t('stock.headers.cogs'), key: 'cogs' },
  { title: t('stock.headers.remarks'), key: 'remarks' },
])

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  try {
    rows.value = await getStockProducts({ keyword: keyword.value, take: 100 })
  } finally {
    loading.value = false
  }
}

function formatMoney(value: number) {
  return formatCurrency(value)
}
</script>
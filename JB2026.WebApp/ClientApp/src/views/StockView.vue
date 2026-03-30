<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">Stock products</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">Modernized stock register powered by the new /api/v2/stock/products endpoint.</p>
        </div>
        <v-spacer />
        <v-text-field
          v-model="keyword"
          density="comfortable"
          label="Search products"
          prepend-inner-icon="mdi-magnify"
          variant="solo-filled"
          hide-details
        />
        <v-btn color="primary" @click="load">Search</v-btn>
      </v-card-title>
      <v-card-text>
        <v-data-table
          :headers="headers"
          :items="rows"
          :loading="loading"
          item-value="productId"
        >
          <template #item.sellingPrice="{ item }">
            {{ formatMoney(item.sellingPrice) }}
          </template>
          <template #item.cogs="{ item }">
            {{ formatMoney(item.cogs) }}
          </template>
          <template #item.balance="{ item }">
            <v-chip size="small" color="secondary" variant="tonal">{{ item.balance }}</v-chip>
          </template>
        </v-data-table>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getStockProducts } from '@/services/stock'
import type { StockProductListItem } from '@/types/api'

const rows = ref<StockProductListItem[]>([])
const loading = ref(false)
const keyword = ref('')

const headers = [
  { title: 'Stock No.', key: 'stockNumber' },
  { title: 'Code', key: 'productCode' },
  { title: 'Product', key: 'productName' },
  { title: 'Balance', key: 'balance' },
  { title: 'Selling Price', key: 'sellingPrice' },
  { title: 'COGS', key: 'cogs' },
  { title: 'Remarks', key: 'remarks' },
]

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
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' }).format(value)
}
</script>
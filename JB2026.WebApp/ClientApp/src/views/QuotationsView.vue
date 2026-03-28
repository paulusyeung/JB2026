<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">Quotation register</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">Sort, filter, and pagination using the same Vuetify server-table baseline as other read-only slices.</p>
        </div>
        <v-spacer />
        <v-text-field
          v-model="store.keyword"
          density="comfortable"
          label="Search quotations"
          prepend-inner-icon="mdi-magnify"
          variant="solo-filled"
          hide-details
        />
        <v-btn color="primary" @click="store.search">Search</v-btn>
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
          <template #item.quotedOn="{ item }">
            {{ formatDate(item.quotedOn) }}
          </template>
          <template #item.totalCostA="{ item }">
            {{ formatMoney(item.totalCostA) }}
          </template>
          <template #item.unitCostA="{ item }">
            {{ formatMoney(item.unitCostA) }}
          </template>
          <template #item.status="{ item }">
            <v-chip size="small" color="accent" variant="tonal">Status {{ item.status }}</v-chip>
          </template>
        </v-data-table-server>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useQuotationsStore } from '@/stores/quotations'

const store = useQuotationsStore()

const headers = [
  { title: 'Quote', key: 'quoteNumberIndexPair' },
  { title: 'Customer', key: 'customerName' },
  { title: 'Title', key: 'printTitle' },
  { title: 'Quoted On', key: 'quotedOn' },
  { title: 'Quoted By', key: 'quotedBy' },
  { title: 'Total', key: 'totalCostA' },
  { title: 'Unit', key: 'unitCostA' },
  { title: 'Status', key: 'status' },
]

onMounted(async () => {
  if (store.rows.length === 0) {
    await store.load()
  }
})

function formatDate(value: string) {
  return new Date(value).toLocaleDateString()
}

function formatMoney(value: number) {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' }).format(value)
}
</script>
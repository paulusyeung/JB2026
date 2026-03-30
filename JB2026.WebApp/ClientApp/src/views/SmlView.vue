<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">SML statistics</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">Monthly and customer aggregates for SML workloads from the modern API.</p>
        </div>
        <v-spacer />
        <v-text-field
          v-model="startOn"
          label="Start date"
          type="date"
          density="comfortable"
          variant="solo-filled"
          hide-details
        />
        <v-btn color="primary" :loading="loading" @click="load">Refresh</v-btn>
      </v-card-title>
      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">
          {{ errorMessage }}
        </v-alert>

        <div v-if="stats" class="d-flex flex-wrap ga-2 mb-4">
          <v-chip color="secondary" variant="tonal">Rows: {{ stats.rowCount }}</v-chip>
          <v-chip color="accent" variant="tonal">Total: {{ formatMoney(stats.totalAmount) }}</v-chip>
        </div>

        <h4 class="text-subtitle-1 mb-2">Monthly totals</h4>
        <v-data-table :headers="monthlyHeaders" :items="stats?.monthly ?? []" :loading="loading">
          <template #item.amount="{ item }">
            {{ formatMoney(item.amount) }}
          </template>
        </v-data-table>

        <h4 class="text-subtitle-1 mt-6 mb-2">Top customers</h4>
        <v-data-table :headers="customerHeaders" :items="stats?.topCustomers ?? []" :loading="loading">
          <template #item.amount="{ item }">
            {{ formatMoney(item.amount) }}
          </template>
        </v-data-table>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getSmlStats } from '@/services/sml'
import type { SmlStatsResponse } from '@/types/api'

const loading = ref(false)
const errorMessage = ref('')
const stats = ref<SmlStatsResponse | null>(null)
const startOn = ref(new Date().toISOString().slice(0, 10))

const monthlyHeaders = [
  { title: 'Year', key: 'year' },
  { title: 'Month', key: 'month' },
  { title: 'Count', key: 'count' },
  { title: 'Amount', key: 'amount' },
]

const customerHeaders = [
  { title: 'Customer', key: 'customerName' },
  { title: 'Count', key: 'count' },
  { title: 'Amount', key: 'amount' },
]

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''

  try {
    stats.value = await getSmlStats({
      startOn: startOn.value,
      days: 31,
      take: 500,
    })
  } catch {
    errorMessage.value = 'Unable to load SML statistics. Please verify API availability.'
  } finally {
    loading.value = false
  }
}

function formatMoney(value: number) {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' }).format(value)
}
</script>
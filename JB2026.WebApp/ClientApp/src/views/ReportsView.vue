<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">Reports runner</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">Runs the exceptional quotation report through the modern report contract.</p>
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
        <v-btn color="primary" :loading="loading" @click="run">Run report</v-btn>
      </v-card-title>
      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">
          {{ errorMessage }}
        </v-alert>

        <div v-if="result" class="d-flex flex-wrap ga-2 mb-4">
          <v-chip color="secondary" variant="tonal">Rows: {{ result.totalRows }}</v-chip>
          <v-chip color="accent" variant="tonal">Total A: {{ formatMoney(result.totalCostA) }}</v-chip>
          <v-chip variant="outlined">{{ result.reportName }}</v-chip>
        </div>

        <v-data-table
          :headers="headers"
          :items="result?.rows ?? []"
          :loading="loading"
          item-value="headerId"
        >
          <template #item.quotedOn="{ item }">
            {{ formatDate(item.quotedOn) }}
          </template>
          <template #item.totalCostA="{ item }">
            {{ formatMoney(item.totalCostA) }}
          </template>
        </v-data-table>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { runReport } from '@/services/reports'
import type { ReportRunResponse } from '@/types/api'

const loading = ref(false)
const errorMessage = ref('')
const result = ref<ReportRunResponse | null>(null)
const startOn = ref(new Date().toISOString().slice(0, 10))

const headers = [
  { title: 'Quote', key: 'quoteNumberIndexPair' },
  { title: 'Customer', key: 'customerName' },
  { title: 'Title', key: 'printTitle' },
  { title: 'Quoted On', key: 'quotedOn' },
  { title: 'Quoted By', key: 'quotedBy' },
  { title: 'Total A', key: 'totalCostA' },
]

onMounted(async () => {
  await run()
})

async function run() {
  loading.value = true
  errorMessage.value = ''

  try {
    result.value = await runReport({
      reportName: 'Exceptional_Report',
      startOn: startOn.value,
      days: 31,
      take: 100,
    })
  } catch {
    errorMessage.value = 'Unable to run report. Please verify API availability.'
  } finally {
    loading.value = false
  }
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString()
}

function formatMoney(value: number) {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' }).format(value)
}
</script>
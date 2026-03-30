<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('reports.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('reports.subtitle') }}</p>
        </div>
        <v-spacer />
        <v-text-field
          v-model="startOn"
          :label="t('reports.startDate')"
          type="date"
          density="comfortable"
          variant="solo-filled"
          hide-details
        />
        <v-btn color="primary" :loading="loading" @click="run">{{ t('reports.runReport') }}</v-btn>
      </v-card-title>
      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">
          {{ errorMessage }}
        </v-alert>

        <div v-if="result" class="d-flex flex-wrap ga-2 mb-4">
          <v-chip color="secondary" variant="tonal">{{ t('reports.rows', { count: result.totalRows }) }}</v-chip>
          <v-chip color="accent" variant="tonal">{{ t('reports.totalA', { amount: formatMoney(result.totalCostA) }) }}</v-chip>
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
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { runReport } from '@/services/reports'
import type { ReportRunResponse } from '@/types/api'

const loading = ref(false)
const errorMessage = ref('')
const result = ref<ReportRunResponse | null>(null)
const startOn = ref(new Date().toISOString().slice(0, 10))
const { t } = useI18n({ useScope: 'global' })
const { formatDate: formatDateByLocale, formatCurrency } = useLocaleFormatters()

const headers = computed(() => [
  { title: t('reports.headers.quote'), key: 'quoteNumberIndexPair' },
  { title: t('reports.headers.customer'), key: 'customerName' },
  { title: t('reports.headers.title'), key: 'printTitle' },
  { title: t('reports.headers.quotedOn'), key: 'quotedOn' },
  { title: t('reports.headers.quotedBy'), key: 'quotedBy' },
  { title: t('reports.headers.totalA'), key: 'totalCostA' },
])

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
    errorMessage.value = t('reports.runFailed')
  } finally {
    loading.value = false
  }
}

function formatDate(value: string) {
  return formatDateByLocale(value)
}

function formatMoney(value: number) {
  return formatCurrency(value)
}
</script>
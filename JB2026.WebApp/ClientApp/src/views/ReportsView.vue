<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="reports-toolbar d-flex flex-wrap align-center ga-3">
        <v-text-field
          v-model="startOn"
          class="reports-toolbar__date"
          :label="t('reports.startDate')"
          type="date"
          density="comfortable"
          variant="solo-filled"
          hide-details
        />
        <v-btn color="primary" :loading="loading" class="reports-toolbar__run" @click="run">{{ t('reports.runReport') }}</v-btn>
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

        <ListMobileCard
          v-if="isPhoneLayout"
          :items="result?.rows ?? []"
          :columns="mobileColumns"
          item-key="headerId"
          :on-card-click="() => undefined"
        />

        <v-data-table
          v-else
          :headers="headers"
          :items="result?.rows ?? []"
          :loading="loading"
          item-value="headerId"
        >
          <template #[`item.quotedOn`]="{ item }">
            {{ format(item.quotedOn) }}
          </template>
          <template #[`item.totalCostA`]="{ item }">
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
import ListMobileCard, { type ListMobileCardColumn } from '@/components/grids/ListMobileCard.vue'
import { useResponsiveList } from '@/composables/useResponsiveList'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { runReport } from '@/services/reports'
import type { QuotationListItem, ReportRunResponse } from '@/types/api'

const loading = ref(false)
const errorMessage = ref('')
const result = ref<ReportRunResponse | null>(null)
const startOn = ref(new Date().toISOString().slice(0, 10))
const { t } = useI18n({ useScope: 'global' })
const { format } = useGlobalDateFormatter()
const { formatCurrency } = useLocaleFormatters()
const { isPhoneLayout } = useResponsiveList()

const headers = computed(() => [
  { title: t('reports.headers.quote'), key: 'quoteNumberIndexPair' },
  { title: t('reports.headers.customer'), key: 'customerName' },
  { title: t('reports.headers.title'), key: 'printTitle' },
  { title: t('reports.headers.quotedOn'), key: 'quotedOn' },
  { title: t('reports.headers.quotedBy'), key: 'quotedBy' },
  { title: t('reports.headers.totalA'), key: 'totalCostA' },
])

const mobileColumns = computed<ListMobileCardColumn<QuotationListItem>[]>(() => [
  { key: 'quoteNumberIndexPair', label: t('reports.headers.quote'), section: 'header', emphasis: true },
  { key: 'customerName', label: t('reports.headers.customer'), section: 'header' },
  { key: 'printTitle', label: t('reports.headers.title'), section: 'body' },
  {
    key: 'quotedOn',
    label: t('reports.headers.quotedOn'),
    section: 'footer',
    formatter: (item) => format(item.quotedOn),
  },
  {
    key: 'totalCostA',
    label: t('reports.headers.totalA'),
    section: 'footer',
    formatter: (item) => formatMoney(item.totalCostA),
  },
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



function formatMoney(value: number) {
  return formatCurrency(value)
}
</script>

<style scoped>
.reports-toolbar__date {
  min-width: 220px;
}

@media (max-width: 960px) {
  .reports-toolbar__date,
  .reports-toolbar__run {
    width: 100%;
  }
}
</style>
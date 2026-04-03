<template>
  <section class="page-section job-stats-page" :class="{ 'job-stats-page--dark': themeStore.isDark }">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('jobOrder.jobStats.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('jobOrder.jobStats.subtitle') }}</p>
        </div>
      </v-card-title>

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('jobOrder.jobStats.lookup')"
            prepend-inner-icon="mdi-magnify"
            variant="solo-filled"
            hide-details
            clearable
            @keydown.enter="refresh"
          />

          <v-text-field
            v-model="startOn"
            type="date"
            density="comfortable"
            :label="t('jobOrder.jobStats.startDate')"
            variant="solo-filled"
            hide-details
          />

          <v-text-field
            v-model="endOn"
            type="date"
            density="comfortable"
            :label="t('jobOrder.jobStats.endDate')"
            variant="solo-filled"
            hide-details
          />

          <v-select
            v-model="rowField"
            :items="rowFieldItems"
            item-title="label"
            item-value="value"
            density="comfortable"
            :label="t('jobOrder.jobStats.rowField')"
            variant="solo-filled"
            hide-details
          />

          <v-select
            v-model="measure"
            :items="measureItems"
            item-title="label"
            item-value="value"
            density="comfortable"
            :label="t('jobOrder.jobStats.measure')"
            variant="solo-filled"
            hide-details
          />

          <v-select
            v-model="month"
            :items="monthItems"
            item-title="label"
            item-value="value"
            density="comfortable"
            :label="t('jobOrder.jobStats.monthFilter')"
            variant="solo-filled"
            hide-details
          />

          <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loading" @click="refresh">
            {{ t('jobOrder.jobStats.search') }}
          </v-btn>

          <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="load">
            {{ t('common.refresh') }}
          </v-btn>

          <v-btn
            variant="outlined"
            prepend-icon="mdi-microsoft-excel"
            :disabled="yearColumns.length === 0"
            @click="exportToCsv"
          >
            {{ t('jobOrder.jobStats.exportToExcel') }}
          </v-btn>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mt-3 mb-2">{{ errorMessage }}</v-alert>

        <div class="text-caption text-medium-emphasis mt-2 mb-2">
          {{ t('jobOrder.jobStats.rows', { count: formatNumber(filteredRows.length) }) }}
        </div>

        <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-3" />

        <div class="pivot-shell">
          <table class="pivot-table" v-if="yearColumns.length > 0">
            <thead>
              <tr>
                <th class="sticky-col">{{ rowFieldLabel }}</th>
                <th v-for="yearValue in yearColumns" :key="yearValue">{{ yearValue }}</th>
                <th>{{ t('jobOrder.jobStats.grandTotal') }}</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="group in pagedGroups" :key="group.key">
                <td class="sticky-col label-cell">{{ group.label }}</td>
                <td v-for="yearValue in yearColumns" :key="`${group.key}-${yearValue}`" class="numeric-cell">
                  {{ formatMeasure(valueFromAggregate(group.byYear[yearValue])) }}
                </td>
                <td class="numeric-cell total-cell">{{ formatMeasure(valueFromAggregate(group.total)) }}</td>
              </tr>
            </tbody>

            <tfoot>
              <tr>
                <th class="sticky-col">{{ t('jobOrder.jobStats.grandTotal') }}</th>
                <th v-for="yearValue in yearColumns" :key="`total-${yearValue}`" class="numeric-cell">
                  {{ formatMeasure(valueFromAggregate(grandByYear[yearValue])) }}
                </th>
                <th class="numeric-cell">{{ formatMeasure(valueFromAggregate(grandTotal)) }}</th>
              </tr>
            </tfoot>
          </table>

          <div v-else class="text-body-2 text-medium-emphasis py-6 text-center">
            {{ t('jobOrder.jobStats.empty') }}
          </div>
        </div>

        <div class="pager-row" v-if="totalPages > 1">
          <div class="text-caption text-medium-emphasis">
            {{ t('jobOrder.jobStats.page', { page, pages: totalPages, count: allGroups.length }) }}
          </div>
          <v-pagination v-model="page" :length="totalPages" density="comfortable" rounded="circle" total-visible="7" />
        </div>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { useThemeStore } from '@/stores/theme'
import { getJobStats } from '@/services/jobOrders'
import type { JobStatsRecord } from '@/types/api'

type RowField = 'salesRep' | 'customerName' | 'brand'
type Measure = 'invoiceAmount' | 'cost' | 'grossProfit'

type Aggregate = {
  invoiceAmount: number
  cost: number
}

type GroupRow = {
  key: string
  label: string
  byYear: Record<number, Aggregate>
  total: Aggregate
}

const rows = ref<JobStatsRecord[]>([])
const loading = ref(false)
const errorMessage = ref('')
const lookup = ref('')
const rowField = ref<RowField>('salesRep')
const measure = ref<Measure>('invoiceAmount')
const month = ref<number | null>(null)
const page = ref(1)

const startOn = ref('')
const endOn = ref('')
const rowsPerPage = 15
const themeStore = useThemeStore()

const { t, locale } = useI18n({ useScope: 'global' })
const { formatCurrency, formatNumber } = useLocaleFormatters()

const rowFieldItems = computed(() => [
  { value: 'salesRep', label: t('jobOrder.jobStats.rowFields.salesRep') },
  { value: 'customerName', label: t('jobOrder.jobStats.rowFields.customerName') },
  { value: 'brand', label: t('jobOrder.jobStats.rowFields.brand') },
])

const measureItems = computed(() => [
  { value: 'invoiceAmount', label: t('jobOrder.jobStats.measures.invoiceAmount') },
  { value: 'cost', label: t('jobOrder.jobStats.measures.cost') },
  { value: 'grossProfit', label: t('jobOrder.jobStats.measures.grossProfit') },
])

const monthItems = computed(() => {
  const formatter = new Intl.DateTimeFormat(locale.value === 'zh-Hans' ? 'zh-CN' : locale.value === 'zh-Hant' ? 'zh-TW' : 'en', {
    month: 'short',
  })

  return [
    { value: null, label: t('jobOrder.jobStats.month.all') },
    ...Array.from({ length: 12 }, (_, index) => {
      const monthNumber = index + 1
      const date = new Date(Date.UTC(2026, index, 1))
      return { value: monthNumber, label: `${monthNumber} - ${formatter.format(date)}` }
    }),
  ]
})

const rowFieldLabel = computed(() => rowFieldItems.value.find((item) => item.value === rowField.value)?.label ?? '')

const filteredRows = computed(() => {
  const token = lookup.value.trim().toLowerCase()

  return rows.value.filter((row) => {
    if (month.value !== null && row.month !== month.value) {
      return false
    }

    if (!token) {
      return true
    }

    return [
      row.jobNumber,
      row.customerName,
      row.brand,
      row.purchaseOrder,
      row.salesRep,
      row.invNumber,
      row.invDate ?? '',
    ].some((field) => field.toLowerCase().includes(token))
  })
})

const yearColumns = computed(() => {
  return Array.from(new Set(filteredRows.value.map((row) => row.year).filter((year): year is number => typeof year === 'number')))
    .sort((left, right) => left - right)
})

const allGroups = computed<GroupRow[]>(() => {
  const byGroup = new Map<string, GroupRow>()

  for (const row of filteredRows.value) {
    const rawLabel = readRowField(row, rowField.value)
    const label = rawLabel.trim().length > 0 ? rawLabel : t('jobOrder.jobStats.blank')
    const key = label.toLowerCase()
    const yearValue = row.year

    let group = byGroup.get(key)
    if (!group) {
      group = {
        key,
        label,
        byYear: {},
        total: createEmptyAggregate(),
      }
      byGroup.set(key, group)
    }

    addToAggregate(group.total, row)

    if (typeof yearValue === 'number') {
      if (!group.byYear[yearValue]) {
        group.byYear[yearValue] = createEmptyAggregate()
      }
      addToAggregate(group.byYear[yearValue], row)
    }
  }

  return Array.from(byGroup.values()).sort((left, right) => left.label.localeCompare(right.label))
})

const totalPages = computed(() => Math.max(1, Math.ceil(allGroups.value.length / rowsPerPage)))

const pagedGroups = computed(() => {
  const offset = (page.value - 1) * rowsPerPage
  return allGroups.value.slice(offset, offset + rowsPerPage)
})

const grandByYear = computed<Record<number, Aggregate>>(() => {
  const result: Record<number, Aggregate> = {}

  for (const group of allGroups.value) {
    for (const yearValue of yearColumns.value) {
      if (!result[yearValue]) {
        result[yearValue] = createEmptyAggregate()
      }
      const cell = group.byYear[yearValue]
      if (cell) {
        result[yearValue].invoiceAmount += cell.invoiceAmount
        result[yearValue].cost += cell.cost
      }
    }
  }

  return result
})

const grandTotal = computed<Aggregate>(() => {
  const total = createEmptyAggregate()
  for (const group of allGroups.value) {
    total.invoiceAmount += group.total.invoiceAmount
    total.cost += group.total.cost
  }
  return total
})

watch([lookup, month, rowField, measure], () => {
  page.value = 1
})

watch(totalPages, (value) => {
  if (page.value > value) {
    page.value = value
  }
})

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''

  try {
    rows.value = await getJobStats({
      startOn: startOn.value || undefined,
      endOn: endOn.value || undefined,
      take: 20000,
    })
  } catch {
    errorMessage.value = t('jobOrder.jobStats.loadFailed')
  } finally {
    loading.value = false
  }
}

async function refresh() {
  page.value = 1
  await load()
}

function valueFromAggregate(aggregate: Aggregate | undefined): number {
  if (!aggregate) {
    return 0
  }

  if (measure.value === 'invoiceAmount') {
    return aggregate.invoiceAmount
  }

  if (measure.value === 'cost') {
    return aggregate.cost
  }

  if (aggregate.invoiceAmount <= 0) {
    return 0
  }

  return (aggregate.invoiceAmount - aggregate.cost) / aggregate.invoiceAmount
}

function formatMeasure(value: number): string {
  if (measure.value === 'grossProfit') {
    return formatNumber(value, { style: 'percent', minimumFractionDigits: 2, maximumFractionDigits: 2 })
  }

  return formatCurrency(value)
}

function exportToCsv() {
  const header = [rowFieldLabel.value, ...yearColumns.value.map(String), t('jobOrder.jobStats.grandTotal')]
  const lines = [header.join(',')]

  for (const group of allGroups.value) {
    const row = [
      csvEscape(group.label),
      ...yearColumns.value.map((yearValue) => csvEscape(formatMeasure(valueFromAggregate(group.byYear[yearValue])))),
      csvEscape(formatMeasure(valueFromAggregate(group.total))),
    ]
    lines.push(row.join(','))
  }

  const totalRow = [
    t('jobOrder.jobStats.grandTotal'),
    ...yearColumns.value.map((yearValue) => csvEscape(formatMeasure(valueFromAggregate(grandByYear.value[yearValue])))),
    csvEscape(formatMeasure(valueFromAggregate(grandTotal.value))),
  ]
  lines.push(totalRow.join(','))

  const blob = new Blob([`\uFEFF${lines.join('\n')}`], { type: 'text/csv;charset=utf-8;' })
  const link = document.createElement('a')
  const timestamp = new Date().toISOString().replace(/[-:T]/g, '').slice(0, 12)
  link.href = URL.createObjectURL(blob)
  link.download = `JobStats_${timestamp}.csv`
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(link.href)
}

function readRowField(row: JobStatsRecord, field: RowField): string {
  if (field === 'customerName') {
    return row.customerName
  }

  if (field === 'brand') {
    return row.brand
  }

  return row.salesRep
}

function createEmptyAggregate(): Aggregate {
  return {
    invoiceAmount: 0,
    cost: 0,
  }
}

function addToAggregate(aggregate: Aggregate, row: JobStatsRecord) {
  aggregate.invoiceAmount += row.invoiceAmount
  aggregate.cost += row.cost
}

function csvEscape(value: string) {
  const escaped = value.replace(/"/g, '""')
  return `"${escaped}"`
}
</script>

<style scoped>
.job-stats-page {
  --pivot-shell-border: rgba(var(--v-theme-on-surface), 0.22);
  --pivot-shell-bg: rgb(var(--v-theme-surface));
  --pivot-cell-border: rgba(var(--v-theme-on-surface), 0.18);
  --pivot-head-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 86%, rgb(var(--v-theme-surface)) 14%);
  --pivot-body-bg: color-mix(in srgb, rgb(var(--v-theme-surface)) 94%, rgb(var(--v-theme-on-surface)) 6%);
  --pivot-sticky-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 76%, rgb(var(--v-theme-surface)) 24%);
  --pivot-foot-top-border: rgba(var(--v-theme-on-surface), 0.2);
  --pivot-foot-top-width: 1px;
  --pivot-foot-sticky-bg: color-mix(in srgb, rgb(var(--v-theme-primary)) 20%, rgb(var(--v-theme-surface-variant)) 80%);
}

.job-stats-page.job-stats-page--dark {
  --pivot-shell-border: rgba(var(--v-theme-on-surface), 0.26);
  --pivot-shell-bg: rgba(var(--v-theme-surface), 0.88);
  --pivot-cell-border: rgba(var(--v-theme-on-surface), 0.16);
  --pivot-head-bg: rgba(var(--v-theme-surface-variant), 0.94);
  --pivot-body-bg: rgba(var(--v-theme-surface), 0.7);
  --pivot-sticky-bg: rgba(var(--v-theme-surface-variant), 0.82);
  --pivot-foot-top-border: rgba(var(--v-theme-on-surface), 0.28);
  --pivot-foot-top-width: 2px;
  --pivot-foot-sticky-bg: rgba(var(--v-theme-surface-variant), 0.82);
}

.filter-bar {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(190px, 1fr));
  gap: 12px;
  align-items: center;
}

.pivot-shell {
  overflow-x: auto;
  border: 1px solid var(--pivot-shell-border);
  border-radius: 10px;
  background: var(--pivot-shell-bg);
}

.pivot-table {
  width: 100%;
  border-collapse: collapse;
  min-width: 860px;
}

.pivot-table th,
.pivot-table td {
  border: 1px solid var(--pivot-cell-border);
  padding: 7px 10px;
  white-space: nowrap;
  font-size: 0.84rem;
}

.pivot-table thead th,
.pivot-table tfoot th {
  background: var(--pivot-head-bg);
  font-weight: 600;
}

.pivot-table tbody td {
  background: var(--pivot-body-bg);
}

.pivot-table .sticky-col {
  position: sticky;
  left: 0;
  z-index: 1;
  background: var(--pivot-sticky-bg);
}

.pivot-table tfoot th {
  border-top: var(--pivot-foot-top-width) solid var(--pivot-foot-top-border);
}

.pivot-table tfoot .sticky-col {
  background: var(--pivot-foot-sticky-bg);
}

@supports not (background: color-mix(in srgb, black, white)) {
  .job-stats-page {
    --pivot-head-bg: rgba(var(--v-theme-surface-variant), 0.9);
    --pivot-body-bg: rgba(var(--v-theme-surface), 0.92);
    --pivot-sticky-bg: rgba(var(--v-theme-surface-variant), 0.82);
    --pivot-foot-sticky-bg: rgba(var(--v-theme-surface-variant), 0.88);
  }

  .job-stats-page.job-stats-page--dark {
    --pivot-head-bg: rgba(var(--v-theme-surface-variant), 0.94);
    --pivot-body-bg: rgba(var(--v-theme-surface), 0.7);
    --pivot-sticky-bg: rgba(var(--v-theme-surface-variant), 0.82);
    --pivot-foot-sticky-bg: rgba(var(--v-theme-surface-variant), 0.82);
  }
}

.pivot-table .label-cell {
  font-weight: 500;
}

.pivot-table .numeric-cell {
  text-align: right;
  font-variant-numeric: tabular-nums;
}

.pivot-table .total-cell {
  font-weight: 600;
}

.pager-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-top: 10px;
}

@media (max-width: 900px) {
  .pager-row {
    flex-direction: column;
    align-items: stretch;
  }
}
</style>

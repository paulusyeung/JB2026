<template>
  <section class="page-section sml-rtf-stats-page">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('sml.rtfStats.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('sml.rtfStats.subtitle') }}</p>
        </div>
      </v-card-title>

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('sml.rtfStats.lookup')"
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
            :label="t('sml.rtfStats.startDate')"
            variant="solo-filled"
            hide-details
          />

          <v-text-field
            v-model="endOn"
            type="date"
            density="comfortable"
            :label="t('sml.rtfStats.endDate')"
            variant="solo-filled"
            hide-details
          />

          <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loading" @click="refresh">
            {{ t('common.search') }}
          </v-btn>

          <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="load">
            {{ t('common.refresh') }}
          </v-btn>

          <v-btn
            variant="outlined"
            prepend-icon="mdi-microsoft-excel"
            :disabled="columnKeys.length === 0"
            @click="exportToCsv"
          >
            {{ t('sml.rtfStats.exportToExcel') }}
          </v-btn>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mt-3 mb-2">{{ errorMessage }}</v-alert>

        <div class="text-caption text-medium-emphasis mt-2 mb-2">
          {{ t('sml.rtfStats.rows', { count: formatNumber(rows.length) }) }}
        </div>

        <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-3" />

        <div class="pivot-shell">
          <table class="pivot-table" v-if="columnKeys.length > 0">
            <thead>
              <tr>
                <th class="sticky-col-1">{{ t('sml.rtfStats.headers.purchaseOrder') }}</th>
                <th class="sticky-col-2">{{ t('sml.rtfStats.headers.productCode') }}</th>
                <th class="sticky-col-3">{{ t('sml.rtfStats.headers.price') }}</th>
                <th class="sticky-col-4">{{ t('sml.rtfStats.headers.qty') }}</th>
                <th v-for="column in columnKeys" :key="column">{{ formatColumnLabel(column) }}</th>
                <th>{{ t('sml.rtfStats.headers.grandTotal') }}</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="group in pagedGroups" :key="group.key">
                <td class="sticky-col-1 label-cell">{{ group.purchaseOrder }}</td>
                <td class="sticky-col-2 label-cell">{{ group.productCode }}</td>
                <td class="sticky-col-3 numeric-cell">{{ group.price }}</td>
                <td class="sticky-col-4 numeric-cell">{{ group.qty }}</td>
                <td v-for="column in columnKeys" :key="`${group.key}-${column}`" class="numeric-cell">
                  {{ formatAmount(group.byColumn[column] ?? 0) }}
                </td>
                <td class="numeric-cell total-cell">{{ formatAmount(group.total) }}</td>
              </tr>
            </tbody>

            <tfoot>
              <tr>
                <th class="sticky-col-1" colspan="4">{{ t('sml.rtfStats.headers.grandTotal') }}</th>
                <th v-for="column in columnKeys" :key="`total-${column}`" class="numeric-cell">
                  {{ formatAmount(grandByColumn[column] ?? 0) }}
                </th>
                <th class="numeric-cell">{{ formatAmount(grandTotal) }}</th>
              </tr>
            </tfoot>
          </table>

          <div v-else class="text-body-2 text-medium-emphasis py-6 text-center">
            {{ t('sml.rtfStats.empty') }}
          </div>
        </div>

        <div class="pager-row" v-if="totalPages > 1">
          <div class="text-caption text-medium-emphasis">
            {{ t('sml.rtfStats.page', { page, pages: totalPages, count: groups.length }) }}
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
import { getSmlRtfStats } from '@/services/sml'
import type { SmlRtfStatsRow } from '@/types/api'

type PivotGroup = {
  key: string
  purchaseOrder: string
  productCode: string
  price: string
  qty: string
  byColumn: Record<string, number>
  total: number
}

const { t } = useI18n({ useScope: 'global' })
const { formatNumber } = useLocaleFormatters()

const rows = ref<SmlRtfStatsRow[]>([])
const loading = ref(false)
const errorMessage = ref('')
const lookup = ref('')
const page = ref(1)
const rowsPerPage = 15

const startOn = ref('')
const endOn = ref('')

const columnKeys = computed(() => {
  return Array.from(new Set(rows.value.map((row) => toColumnKey(row.year, row.month)))).sort()
})

const groups = computed<PivotGroup[]>(() => {
  const map = new Map<string, PivotGroup>()

  for (const row of rows.value) {
    const purchaseOrder = row.purchaseOrder.trim() || t('sml.rtfStats.blank')
    const productCode = row.productCode.trim() || t('sml.rtfStats.blank')
    const price = row.price.trim() || '-'
    const qty = row.qty.trim() || '-'
    const key = [purchaseOrder.toLowerCase(), productCode.toLowerCase(), price, qty].join('|')
    const columnKey = toColumnKey(row.year, row.month)

    let group = map.get(key)
    if (!group) {
      group = {
        key,
        purchaseOrder,
        productCode,
        price,
        qty,
        byColumn: {},
        total: 0,
      }
      map.set(key, group)
    }

    group.byColumn[columnKey] = (group.byColumn[columnKey] ?? 0) + row.amount
    group.total += row.amount
  }

  return Array.from(map.values()).sort((left, right) => {
    const poCompare = left.purchaseOrder.localeCompare(right.purchaseOrder)
    if (poCompare !== 0) {
      return poCompare
    }

    const productCompare = left.productCode.localeCompare(right.productCode)
    if (productCompare !== 0) {
      return productCompare
    }

    return left.key.localeCompare(right.key)
  })
})

const totalPages = computed(() => Math.max(1, Math.ceil(groups.value.length / rowsPerPage)))

const pagedGroups = computed(() => {
  const offset = (page.value - 1) * rowsPerPage
  return groups.value.slice(offset, offset + rowsPerPage)
})

const grandByColumn = computed<Record<string, number>>(() => {
  const totals: Record<string, number> = {}

  for (const columnKey of columnKeys.value) {
    totals[columnKey] = 0
  }

  for (const group of groups.value) {
    for (const columnKey of columnKeys.value) {
      const runningTotal = totals[columnKey] ?? 0
      const columnAmount = group.byColumn[columnKey] ?? 0
      totals[columnKey] = runningTotal + columnAmount
    }
  }

  return totals
})

const grandTotal = computed(() => {
  let total = 0
  for (const group of groups.value) {
    total += group.total
  }
  return total
})

watch([lookup, startOn, endOn], () => {
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
    const response = await getSmlRtfStats({
      startOn: startOn.value || undefined,
      endOn: endOn.value || undefined,
      lookup: lookup.value.trim() || undefined,
      take: 20000,
    })

    rows.value = response.rows
  } catch {
    errorMessage.value = t('sml.rtfStats.loadFailed')
  } finally {
    loading.value = false
  }
}

async function refresh() {
  page.value = 1
  await load()
}

function toColumnKey(year: number, month: number): string {
  return `${year}-${month.toString().padStart(2, '0')}`
}

function formatColumnLabel(columnKey: string): string {
  const [yearText, monthText] = columnKey.split('-')
  const year = Number(yearText)
  const month = Number(monthText)

  if (!Number.isFinite(year) || !Number.isFinite(month)) {
    return columnKey
  }

  return `${year}/${month.toString().padStart(2, '0')}`
}

function formatAmount(value: number): string {
  return formatNumber(value, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })
}

function exportToCsv() {
  const header = [
    t('sml.rtfStats.headers.purchaseOrder'),
    t('sml.rtfStats.headers.productCode'),
    t('sml.rtfStats.headers.price'),
    t('sml.rtfStats.headers.qty'),
    ...columnKeys.value.map((column) => formatColumnLabel(column)),
    t('sml.rtfStats.headers.grandTotal'),
  ]

  const lines = [header.map(csvEscape).join(',')]

  for (const group of groups.value) {
    const row = [
      csvEscape(group.purchaseOrder),
      csvEscape(group.productCode),
      csvEscape(group.price),
      csvEscape(group.qty),
      ...columnKeys.value.map((column) => csvEscape(formatAmount(group.byColumn[column] ?? 0))),
      csvEscape(formatAmount(group.total)),
    ]

    lines.push(row.join(','))
  }

  const totalRow = [
    csvEscape(t('sml.rtfStats.headers.grandTotal')),
    csvEscape(''),
    csvEscape(''),
    csvEscape(''),
    ...columnKeys.value.map((column) => csvEscape(formatAmount(grandByColumn.value[column] ?? 0))),
    csvEscape(formatAmount(grandTotal.value)),
  ]

  lines.push(totalRow.join(','))

  const blob = new Blob([`\uFEFF${lines.join('\n')}`], { type: 'text/csv;charset=utf-8;' })
  const link = document.createElement('a')
  const timestamp = new Date().toISOString().replace(/[-:T]/g, '').slice(0, 12)

  link.href = URL.createObjectURL(blob)
  link.download = `RtfStats_${timestamp}.csv`
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(link.href)
}

function csvEscape(value: string): string {
  const escaped = value.replace(/"/g, '""')
  return `"${escaped}"`
}
</script>

<style scoped>
.sml-rtf-stats-page {
  --pivot-shell-border: rgba(var(--v-theme-on-surface), 0.2);
  --pivot-shell-bg: rgb(var(--v-theme-surface));
  --pivot-cell-border: rgba(var(--v-theme-on-surface), 0.15);
  --pivot-head-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 86%, rgb(var(--v-theme-surface)) 14%);
  --pivot-body-bg: color-mix(in srgb, rgb(var(--v-theme-surface)) 95%, rgb(var(--v-theme-on-surface)) 5%);
  --pivot-sticky-bg: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 76%, rgb(var(--v-theme-surface)) 24%);
}

.filter-bar {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(190px, 1fr));
  gap: 12px;
  align-items: center;
}

.pivot-shell {
  overflow: auto;
  border: 1px solid var(--pivot-shell-border);
  border-radius: 10px;
  background: var(--pivot-shell-bg);
}

.pivot-table {
  width: 100%;
  border-collapse: collapse;
  min-width: 1200px;
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

.pivot-table .label-cell {
  font-weight: 500;
}

.pivot-table .numeric-cell {
  text-align: right;
}

.pivot-table .total-cell {
  font-weight: 700;
}

.pivot-table .sticky-col-1,
.pivot-table .sticky-col-2,
.pivot-table .sticky-col-3,
.pivot-table .sticky-col-4 {
  position: sticky;
  z-index: 1;
  background: var(--pivot-sticky-bg);
}

.pivot-table .sticky-col-1 {
  left: 0;
}

.pivot-table .sticky-col-2 {
  left: 185px;
}

.pivot-table .sticky-col-3 {
  left: 360px;
}

.pivot-table .sticky-col-4 {
  left: 480px;
}

.pager-row {
  margin-top: 12px;
  display: flex;
  justify-content: space-between;
  gap: 10px;
  align-items: center;
  flex-wrap: wrap;
}

@media (max-width: 900px) {
  .pivot-table .sticky-col-2,
  .pivot-table .sticky-col-3,
  .pivot-table .sticky-col-4 {
    position: static;
  }

  .pivot-table {
    min-width: 900px;
  }
}

@supports not (background: color-mix(in srgb, black, white)) {
  .sml-rtf-stats-page {
    --pivot-head-bg: rgba(var(--v-theme-surface-variant), 0.9);
    --pivot-body-bg: rgba(var(--v-theme-surface), 0.95);
    --pivot-sticky-bg: rgba(var(--v-theme-surface-variant), 0.85);
  }
}
</style>

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

        <div v-if="pivotMounted" class="pivot-shell">
          <web-pivot-table ref="pivotRef" class="pivot-element" />
        </div>

        <div v-else-if="!pivotAvailable" class="text-body-2 text-medium-emphasis py-6 text-center">
          {{ t('sml.rtfStats.loadFailed') }}
        </div>

        <div v-if="!loading && rows.length === 0" class="text-body-2 text-medium-emphasis py-6 text-center">
          {{ t('sml.rtfStats.empty') }}
        </div>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { getSmlRtfStats } from '@/services/sml'
import type { SmlRtfStatsRow } from '@/types/api'

type WptElement = HTMLElement & {
  setLocale?: (locale: string) => void
  setOptions?: (options: Record<string, unknown>) => void
  getSourceObject?: () => Record<string, unknown> | null
  setSourceObject?: (source: Record<string, unknown>) => void
  setWptFromDataArray?: (
    attrArray: string[],
    dataArray: Array<Array<string | number>>,
    url?: string,
    type?: string,
  ) => Promise<void> | void
  configurePivot?: (config: Record<string, unknown>) => void
}

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
const { locale } = useI18n({ useScope: 'global' })
const { formatNumber } = useLocaleFormatters()

const rows = ref<SmlRtfStatsRow[]>([])
const loading = ref(false)
const errorMessage = ref('')
const lookup = ref('')
const pivotRef = ref<WptElement | null>(null)
const pivotMounted = ref(false)
const pivotAvailable = ref(false)

const startOn = ref('')
const endOn = ref('')
let hydrateRetryTimer: number | null = null
let hydrateAttempts = 0
const MAX_HYDRATE_ATTEMPTS = 8

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

onMounted(async () => {
  pivotAvailable.value = await ensurePivotComponentLoaded()
  if (!pivotAvailable.value) {
    errorMessage.value = t('sml.rtfStats.loadFailed')
    return
  }

  await customElements.whenDefined('web-pivot-table')
  await load()

  if (!pivotMounted.value) {
    pivotMounted.value = true
    await nextTick()

    const pivot = pivotRef.value
    if (pivot) {
      pivot.addEventListener('wpt:ready', onPivotReady as EventListener)
    }

    hydrateAttempts = 0
    scheduleHydratePivot()
  }
})

onUnmounted(() => {
  const pivot = pivotRef.value
  if (pivot) {
    pivot.removeEventListener('wpt:ready', onPivotReady as EventListener)
  }

  if (hydrateRetryTimer != null) {
    window.clearTimeout(hydrateRetryTimer)
    hydrateRetryTimer = null
  }
})

async function load() {
  if (!pivotAvailable.value) {
    return
  }

  loading.value = true
  errorMessage.value = ''

  try {
    const response = await getSmlRtfStats({
      startOn: startOn.value || undefined,
      endOn: endOn.value || undefined,
      lookup: lookup.value.trim() || undefined,
    })

    rows.value = response.rows
    if (pivotMounted.value) {
      await nextTick()
      hydrateAttempts = 0
      scheduleHydratePivot()
    }
  } catch {
    errorMessage.value = t('sml.rtfStats.loadFailed')
  } finally {
    loading.value = false
  }
}

async function refresh() {
  await load()
}

async function hydratePivot() {
  const pivot = pivotRef.value
  if (!pivot || rows.value.length === 0) {
    return false
  }

  const attrArray = [
    'Customer PO',
    'Ordered On',
    'Ordered By',
    'Original PO',
    'Sales Order',
    'Original SO',
    'Purchase Order',
    'Product Code',
    'Price',
    'Qty',
    'Year',
    'Month',
    'Amount',
  ]
  const dataArray = rows.value.map((row) => [
    normalizeText(row.customerPO),
    normalizeText(row.orderedOn),
    normalizeText(row.orderedBy),
    normalizeText(row.originalPO),
    normalizeText(row.salesOrder),
    normalizeText(row.originalSO),
    normalizeText(row.purchaseOrder),
    normalizeText(row.productCode),
    normalizeText(row.price, '-'),
    normalizeText(row.qty, '-'),
    Number(row.year ?? 0),
    Number(row.month ?? 0),
    Number(row.amount ?? 0),
  ])

  try {
    pivot.setLocale?.(locale.value)
    pivot.setOptions?.({
      locale: locale.value,
      layout: { fitMode: 'fill' },
      uiFlags: {
        save: true,
        saveToLocal: true,
      },
    })

    await pivot.setWptFromDataArray?.(attrArray, dataArray, 'memory://sml-rtf-stats')

    const source = pivot.getSourceObject?.()
    if (source && source.mode === 'MEMORY' && (!source.url || typeof source.url !== 'string')) {
      pivot.setSourceObject?.({
        ...source,
        url: 'memory://sml-rtf-stats',
      })
    }

    pivot.configurePivot?.({
      displayMode: 'grid',
      filters: ['Customer PO', 'Ordered On', 'Ordered By', 'Original PO', 'Sales Order', 'Original SO'],
      rows: ['Purchase Order', 'Product Code', 'Price', 'Qty'],
      columns: ['Year', 'Month'],
      values: [{
        field: 'Amount',
        aggregation: 'SUM',
        formatter: (value: unknown) => {
          if (typeof value !== 'number') return String(value)
          return formatNumber(value, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
        },
      }],
      sort: {
        field: 'Purchase Order',
        direction: 'asc',
      },
      grid: {
        compactForm: false,
      },
      showRowTotals: true,
      showColTotals: true,
    })
    return !hasNoStoreError(pivot)
  } catch {
    errorMessage.value = t('sml.rtfStats.loadFailed')
    return false
  }
}

function onPivotReady() {
  hydrateAttempts = 0
  scheduleHydratePivot()
}

async function scheduleHydratePivot() {
  if (hydrateRetryTimer != null) {
    window.clearTimeout(hydrateRetryTimer)
    hydrateRetryTimer = null
  }

  if (!pivotAvailable.value || rows.value.length === 0) {
    return
  }

  const pivot = pivotRef.value
  if (!pivot) {
    if (hydrateAttempts >= MAX_HYDRATE_ATTEMPTS) {
      errorMessage.value = t('sml.rtfStats.loadFailed')
      return
    }

    hydrateAttempts += 1
    hydrateRetryTimer = window.setTimeout(scheduleHydratePivot, 250)
    return
  }

  const hasMethods = typeof pivot.setWptFromDataArray === 'function'
  if (!hasMethods) {
    if (hydrateAttempts >= MAX_HYDRATE_ATTEMPTS) {
      errorMessage.value = t('sml.rtfStats.loadFailed')
      return
    }

    hydrateAttempts += 1
    hydrateRetryTimer = window.setTimeout(scheduleHydratePivot, 250)
    return
  }

  const healthy = hasMethods && (await hydratePivot())

  if (healthy) {
    return
  }

  if (hydrateAttempts >= MAX_HYDRATE_ATTEMPTS) {
    errorMessage.value = t('sml.rtfStats.loadFailed')
    return
  }

  hydrateAttempts += 1
  hydrateRetryTimer = window.setTimeout(scheduleHydratePivot, 250)
}

function hasNoStoreError(pivot: WptElement | null): boolean {
  if (!pivot) {
    return false
  }

  const text = `${pivot.textContent || ''} ${pivot.shadowRoot?.textContent || ''}`
  return text.includes('No store for')
}

function normalizeText(value: unknown, fallback = t('sml.rtfStats.blank')): string {
  if (typeof value !== 'string') {
    return fallback
  }

  const trimmed = value.trim()
  return trimmed || fallback
}

async function ensurePivotComponentLoaded(): Promise<boolean> {
  if (customElements.get('web-pivot-table')) {
    return true
  }

  try {
    await import('webpivottable-dist')
  } catch {
    return false
  }

  return Boolean(customElements.get('web-pivot-table'))
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
}

.filter-bar {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(190px, 1fr));
  gap: 12px;
  align-items: center;
}

.pivot-shell {
  overflow: hidden;
  border: 1px solid var(--pivot-shell-border);
  border-radius: 10px;
  background: var(--pivot-shell-bg);
  height: clamp(560px, calc(100vh - 340px), 720px);
}

.pivot-element {
  display: block;
  width: 100%;
  height: 100%;
  min-height: 560px;
}

</style>

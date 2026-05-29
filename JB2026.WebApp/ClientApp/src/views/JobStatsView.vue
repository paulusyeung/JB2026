<template>
  <section class="page-section job-stats-page">
    <v-card rounded="xl" elevation="0" class="panel-card">


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
            :disabled="filteredRows.length === 0"
            @click="exportToCsv"
          >
            {{ t('jobOrder.jobStats.exportToExcel') }}
          </v-btn>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mt-3 mb-2">{{ errorMessage }}</v-alert>

        <v-alert
          v-if="isNarrowPhoneLayout"
          type="info"
          variant="tonal"
          density="compact"
          class="mt-2 mb-3"
        >
          {{ t('jobOrder.jobStats.mobilePreferredNotice') }}
        </v-alert>

        <div class="text-caption text-medium-emphasis mt-2 mb-2">
          {{ t('jobOrder.jobStats.rows', { count: formatNumber(filteredRows.length) }) }}
        </div>

        <v-row v-if="isPhoneLayout" dense class="mb-3">
          <v-col cols="6">
            <v-card rounded="lg" variant="flat" class="pa-3 border">
              <div class="text-caption text-medium-emphasis">{{ t('jobOrder.jobStats.summary.rows') }}</div>
              <div class="text-h6 font-weight-bold">{{ formatNumber(filteredRows.length) }}</div>
            </v-card>
          </v-col>
          <v-col cols="6">
            <v-card rounded="lg" variant="flat" class="pa-3 border">
              <div class="text-caption text-medium-emphasis">{{ t('jobOrder.jobStats.summary.invoiceAmount') }}</div>
              <div class="text-h6 font-weight-bold">{{ formatSummaryCurrency(totalInvoiceAmount) }}</div>
            </v-card>
          </v-col>
          <v-col cols="6">
            <v-card rounded="lg" variant="flat" class="pa-3 border">
              <div class="text-caption text-medium-emphasis">{{ t('jobOrder.jobStats.summary.cost') }}</div>
              <div class="text-h6 font-weight-bold">{{ formatSummaryCurrency(totalCost) }}</div>
            </v-card>
          </v-col>
          <v-col cols="6">
            <v-card rounded="lg" variant="flat" class="pa-3 border">
              <div class="text-caption text-medium-emphasis">{{ t('jobOrder.jobStats.summary.grossProfit') }}</div>
              <div class="text-h6 font-weight-bold">{{ formatNumber(grossProfitRatioSummary, { style: 'percent', minimumFractionDigits: 2, maximumFractionDigits: 2 }) }}</div>
            </v-card>
          </v-col>
        </v-row>

        <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-3" />

        <v-expansion-panels v-if="isPhoneLayout && !loading" variant="accordion" class="mb-4">
          <v-expansion-panel v-for="group in groupedStats" :key="group.name">
            <v-expansion-panel-title>
              <div class="d-flex justify-space-between align-center w-100">
                <span class="font-weight-bold">{{ group.name }}</span>
                <span class="text-primary font-weight-medium">
                  {{ measure === 'grossProfit' 
                      ? formatNumber(group.total, { style: 'percent', minimumFractionDigits: 2, maximumFractionDigits: 2 }) 
                      : formatSummaryCurrency(group.total) 
                  }}
                </span>
              </div>
            </v-expansion-panel-title>
            <v-expansion-panel-text>
              <v-list density="compact">
                <v-list-item v-for="job in group.jobs" :key="job.jobNumber">
                  <v-list-item-title class="text-caption">{{ job.jobNumber }} - {{ job.customerName }}</v-list-item-title>
                  <v-list-item-subtitle class="text-caption">
                    {{ measure === 'grossProfit' 
                        ? formatNumber(grossProfitRatio(job), { style: 'percent', minimumFractionDigits: 2, maximumFractionDigits: 2 }) 
                        : formatSummaryCurrency(Number(job[measure] ?? 0)) 
                    }}
                  </v-list-item-subtitle>
                </v-list-item>
              </v-list>
            </v-expansion-panel-text>
          </v-expansion-panel>
        </v-expansion-panels>

        <div v-if="!isPhoneLayout && pivotMounted" :class="['pivot-shell', { 'pivot-shell--mobile': isPhoneLayout }]">
          <div class="pivot-shell__scroller">
            <web-pivot-table ref="pivotRef" class="pivot-element" />
          </div>
        </div>

        <div v-else-if="!pivotAvailable" class="text-body-2 text-medium-emphasis py-6 text-center">
          {{ t('jobOrder.jobStats.loadFailed') }}
        </div>

        <div v-if="!loading && filteredRows.length === 0" class="text-body-2 text-medium-emphasis py-6 text-center">
          {{ t('jobOrder.jobStats.empty') }}
        </div>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useDisplay } from 'vuetify'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { getJobStats } from '@/services/jobOrders'
import { useThemeStore } from '@/stores/theme'
import type { JobStatsRecord } from '@/types/api'

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

type RowField = 'salesRep' | 'customerName' | 'brand'
type Measure = 'invoiceAmount' | 'cost' | 'grossProfit'

const rows = ref<JobStatsRecord[]>([])
const loading = ref(false)
const errorMessage = ref('')
const lookup = ref('')
const rowField = ref<RowField>('salesRep')
const measure = ref<Measure>('invoiceAmount')
const month = ref<number | null>(null)
const pivotRef = ref<WptElement | null>(null)
const pivotMounted = ref(false)
const pivotAvailable = ref(false)

const startOn = ref('')
const endOn = ref('')
let hydrateRetryTimer: number | null = null
let hydrateAttempts = 0
const MAX_HYDRATE_ATTEMPTS = 8

const { t, locale } = useI18n({ useScope: 'global' })
const { formatNumber } = useLocaleFormatters()
const display = useDisplay()
const themeStore = useThemeStore()
const isPhoneLayout = computed(() => display.smAndDown.value)
const isNarrowPhoneLayout = computed(() => display.xs.value && display.width.value <= 430)
const webPivotTheme = computed(() => {
  if (themeStore.mode !== 'dark') {
    return {
      preset: 'default',
      colors: {
        primary: '#6366f1',
        primaryText: '#ffffff',
        background: '#ffffff',
        backgroundPanel: '#ffffff',
        backgroundSecondary: '#fafafa',
        backgroundTertiary: '#f5f5f5',
        border: '#d0d4da',
        borderLight: '#e8ecf0',
        text: '#1a1a1a',
        textSecondary: '#555555',
        textMuted: '#888888',
        toolbarBackground: '#eef2f7',
        gridHeaderBackground: '#dde6f0',
        gridHeaderText: '#1a2a3a',
        gridBackground: '#ffffff',
        gridText: '#1a1a1a',
        buttonBackground: '#e7e7e7',
        buttonBorder: '#dcdfe6',
        buttonText: '#1a1a1a',
        gridTotalBackground: '#72d2df',
        gridSubtotalBackground: '#d2e9e9',
      },
      typography: {
        fontSize: '13px',
        lineHeight: 28,
      },
      shape: {
        borderRadius: '6px',
      },
    }
  }

  return {
    preset: 'dark',
    colors: {
      primary: '#e29a60',
      primaryText: '#1e241f',
      background: '#161916',
      backgroundPanel: '#1e241f',
      backgroundSecondary: '#252d26',
      backgroundTertiary: '#2a322b',
      border: '#384338',
      borderLight: '#485348',
      text: '#e7e5dc',
      textSecondary: '#b9c2b3',
      textMuted: '#8f9a8f',
      toolbarBackground: '#2a322b',
      gridHeaderBackground: '#2a322b',
      gridHeaderText: '#e7e5dc',
      gridBackground: '#1e241f',
      gridText: '#e7e5dc',
      buttonBackground: '#2a322b',
      buttonBorder: '#485348',
      buttonText: '#e7e5dc',
      gridTotalBackground: '#2f4f40',
      gridSubtotalBackground: '#2c3f35',
    },
    typography: {
      fontSize: '13px',
      lineHeight: 28,
    },
    shape: {
      borderRadius: '6px',
    },
  }
})

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
  const localeCode = locale.value === 'zh-Hans' ? 'zh-CN' : locale.value === 'zh-Hant' ? 'zh-TW' : 'en'
  const formatter = new Intl.DateTimeFormat(localeCode, { month: 'short' })

  return [
    { value: null, label: t('jobOrder.jobStats.month.all') },
    ...Array.from({ length: 12 }, (_, index) => {
      const monthNumber = index + 1
      const date = new Date(Date.UTC(2026, index, 1))
      return { value: monthNumber, label: `${monthNumber} - ${formatter.format(date)}` }
    }),
  ]
})

const filteredRows = computed(() => {
  const token = lookup.value.trim().toLowerCase()

  return rows.value.filter((row) => {
    if (!isWithinLastTenYears(row)) {
      return false
    }

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
    ].some((field) => String(field).toLowerCase().includes(token))
  }).sort((left, right) => {
    const leftSalesRep = normalizeText(left.salesRep).toLowerCase()
    const rightSalesRep = normalizeText(right.salesRep).toLowerCase()
    const salesRepCompare = leftSalesRep.localeCompare(rightSalesRep)
    if (salesRepCompare !== 0) {
      return salesRepCompare
    }

    const leftJob = normalizeText(left.jobNumber).toLowerCase()
    const rightJob = normalizeText(right.jobNumber).toLowerCase()
    return leftJob.localeCompare(rightJob)
  })
})

const totalInvoiceAmount = computed(() => filteredRows.value.reduce((total, row) => total + Number(row.invoiceAmount ?? 0), 0))
const totalCost = computed(() => filteredRows.value.reduce((total, row) => total + Number(row.cost ?? 0), 0))
const grossProfitRatioSummary = computed(() => {
  if (totalInvoiceAmount.value <= 0) {
    return 0
  }
  return (totalInvoiceAmount.value - totalCost.value) / totalInvoiceAmount.value
})

const groupedStats = computed(() => {
  const groups: Record<string, { total: number; jobs: JobStatsRecord[] }> = {}
  
  filteredRows.value.forEach(row => {
    const groupName = normalizeText(row[rowField.value])
    if (!groups[groupName]) {
      groups[groupName] = { total: 0, jobs: [] }
    }
    
    let value = 0
    if (measure.value === 'invoiceAmount') value = Number(row.invoiceAmount ?? 0)
    else if (measure.value === 'cost') value = Number(row.cost ?? 0)
    else if (measure.value === 'grossProfit') value = grossProfitRatio(row)
    
    groups[groupName].total += value
    groups[groupName].jobs.push(row)
  })
  
  return Object.entries(groups).map(([name, data]) => ({
    name,
    total: data.total,
    jobs: data.jobs
  })).sort((a, b) => b.total - a.total)
})

watch([lookup, month, rowField, measure], async () => {
  if (!pivotMounted.value || !pivotAvailable.value) {
    return
  }

  await nextTick()
  hydrateAttempts = 0
  scheduleHydratePivot()
})

watch(() => themeStore.mode, async () => {
  if (!pivotMounted.value || !pivotAvailable.value) {
    return
  }

  await nextTick()
  hydrateAttempts = 0
  scheduleHydratePivot()
})

onMounted(async () => {
  pivotAvailable.value = await ensurePivotComponentLoaded()
  if (!pivotAvailable.value) {
    errorMessage.value = t('jobOrder.jobStats.loadFailed')
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
    const defaultStartOn = startOn.value || (() => {
      const d = new Date()
      d.setFullYear(d.getFullYear() - 9, 0, 1)
      return d.toISOString().slice(0, 10)
    })()

    rows.value = await getJobStats({
      startOn: defaultStartOn,
      endOn: endOn.value || undefined,
    })

    if (pivotMounted.value) {
      await nextTick()
      hydrateAttempts = 0
      scheduleHydratePivot()
    }
  } catch {
    errorMessage.value = t('jobOrder.jobStats.loadFailed')
  } finally {
    loading.value = false
  }
}

async function refresh() {
  await load()
}

async function hydratePivot() {
  const pivot = pivotRef.value
  if (!pivot || filteredRows.value.length === 0) {
    return false
  }

  const attrArray = [
    'Job Number',
    'Customer Name',
    'Brand',
    'Purchase Order',
    'Sales Rep',
    'Gross Profit',
    'Cost',
    'Invoice Amount',
    'Inv Number',
    'Inv Date',
    'Year',
    'Month',
  ]

  const dataArray = filteredRows.value.map((row) => [
    normalizeText(row.jobNumber),
    normalizeText(row.customerName),
    normalizeText(row.brand),
    normalizeText(row.purchaseOrder),
    normalizeText(row.salesRep),
    grossProfitRatio(row),
    Number(row.cost ?? 0),
    Number(row.invoiceAmount ?? 0),
    normalizeText(row.invNumber),
    normalizeText(row.invDate),
    Number(row.year ?? 0),
    Number(row.month ?? 0),
  ])

  const rowFieldName = rowField.value === 'customerName'
    ? 'Customer Name'
    : rowField.value === 'brand'
      ? 'Brand'
      : 'Sales Rep'

  const valueFieldName = measure.value === 'cost'
    ? 'Cost'
    : measure.value === 'grossProfit'
      ? 'Gross Profit'
      : 'Invoice Amount'

  try {
    pivot.setLocale?.(locale.value)
    pivot.setOptions?.({
      locale: locale.value,
      theme: webPivotTheme.value,
      layout: { fitMode: 'fill' },
      uiFlags: {
        save: true,
        saveToLocal: true,
      },
    })

    await pivot.setWptFromDataArray?.(attrArray, dataArray, 'memory://job-stats')

    const source = pivot.getSourceObject?.()
    if (source && source.mode === 'MEMORY' && (!source.url || typeof source.url !== 'string')) {
      pivot.setSourceObject?.({
        ...source,
        url: 'memory://job-stats',
      })
    }

    pivot.configurePivot?.({
      displayMode: 'grid',
      filters: ['Job Number', 'Customer Name', 'Brand', 'Purchase Order', 'Gross Profit', 'Cost', 'Inv Number', 'Inv Date'],
      rows: [rowFieldName],
      columns: ['Year', 'Month'],
      values: [{
        field: valueFieldName,
        aggregation: 'SUM',
        format: measure.value === 'invoiceAmount'
          ? {
            category: 'CURRENCY',
            decimal: 2,
            separatorFlag: true,
            symbol: '$',
            symbolSuffix: false,
          }
          : measure.value === 'grossProfit'
            ? {
              category: 'PERCENTAGE',
              decimal: 2,
            }
            : {
              category: 'NUMBER',
              decimal: 2,
              separatorFlag: true,
            },
        formatter: (value: unknown) => {
          if (measure.value === 'grossProfit') {
            if (typeof value !== 'number') return String(value)
            return formatNumber(value, { style: 'percent', minimumFractionDigits: 2, maximumFractionDigits: 2 })
          }
          if (measure.value === 'invoiceAmount') {
            return formatInvoiceAmountCurrency(value)
          }
          if (typeof value !== 'number') return String(value)
          return formatNumber(value, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
        },
      }],
      sort: {
        field: 'Sales Rep',
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
    errorMessage.value = t('jobOrder.jobStats.loadFailed')
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

  if (!pivotAvailable.value || filteredRows.value.length === 0) {
    return
  }

  const pivot = pivotRef.value
  if (!pivot) {
    if (hydrateAttempts >= MAX_HYDRATE_ATTEMPTS) {
      errorMessage.value = t('jobOrder.jobStats.loadFailed')
      return
    }

    hydrateAttempts += 1
    hydrateRetryTimer = window.setTimeout(scheduleHydratePivot, 250)
    return
  }

  const hasMethods = typeof pivot.setWptFromDataArray === 'function'
  if (!hasMethods) {
    if (hydrateAttempts >= MAX_HYDRATE_ATTEMPTS) {
      errorMessage.value = t('jobOrder.jobStats.loadFailed')
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
    errorMessage.value = t('jobOrder.jobStats.loadFailed')
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

function grossProfitRatio(row: JobStatsRecord): number {
  const invoiceAmount = Number(row.invoiceAmount ?? 0)
  const cost = Number(row.cost ?? 0)
  if (invoiceAmount <= 0) {
    return 0
  }
  return (invoiceAmount - cost) / invoiceAmount
}

function isWithinLastTenYears(row: JobStatsRecord): boolean {
  const currentYear = new Date().getFullYear()
  const minYear = currentYear - 9
  const year = normalizeToGregorianYear(row.year)

  if (Number.isFinite(year)) {
    return year >= minYear && year <= currentYear
  }

  if (typeof row.invDate === 'string') {
    const parsed = new Date(row.invDate)
    const parsedYear = parsed.getFullYear()
    return Number.isFinite(parsedYear) && parsedYear >= minYear && parsedYear <= currentYear
  }

  return false
}

function normalizeToGregorianYear(value: unknown): number {
  const rawYear = Number(value)
  if (!Number.isFinite(rawYear)) {
    return Number.NaN
  }

  const integerYear = Math.trunc(rawYear)

  // Accept both Gregorian years (2025) and ROC years (114 -> 2025).
  if (integerYear > 0 && integerYear < 300) {
    return integerYear + 1911
  }

  return integerYear
}

function normalizeText(value: unknown): string {
  if (typeof value !== 'string') {
    return t('jobOrder.jobStats.blank')
  }

  const trimmed = value.trim()
  return trimmed || t('jobOrder.jobStats.blank')
}

function formatInvoiceAmountCurrency(value: unknown): string {
  const numericValue = typeof value === 'number' ? value : Number(value)
  if (!Number.isFinite(numericValue)) {
    return String(value ?? '')
  }

  return numericValue.toLocaleString('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })
}

function formatSummaryCurrency(value: number): string {
  return value.toLocaleString('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  })
}

function exportToCsv() {
  const header = [
    'Job Number',
    'Customer Name',
    'Brand',
    'Purchase Order',
    'Sales Rep',
    'Gross Profit %',
    'Cost',
    'Invoice Amount',
    'Inv Number',
    'Inv Date',
    'Year',
    'Month',
  ]

  const lines = [header.map(csvEscape).join(',')]

  for (const row of filteredRows.value) {
    lines.push(
      [
        normalizeText(row.jobNumber),
        normalizeText(row.customerName),
        normalizeText(row.brand),
        normalizeText(row.purchaseOrder),
        normalizeText(row.salesRep),
        formatNumber(grossProfitRatio(row), { style: 'percent', minimumFractionDigits: 2, maximumFractionDigits: 2 }),
        formatNumber(row.cost ?? 0, { minimumFractionDigits: 2, maximumFractionDigits: 2 }),
        formatInvoiceAmountCurrency(row.invoiceAmount ?? 0),
        normalizeText(row.invNumber),
        normalizeText(row.invDate),
        String(row.year ?? 0),
        String(row.month ?? 0),
      ]
        .map((value) => csvEscape(value ?? ''))
        .join(','),
    )
  }

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

function csvEscape(value: unknown): string {
  const escaped = String(value).replace(/"/g, '""')
  return `"${escaped}"`
}
</script>

<style scoped>
.job-stats-page {
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
  overflow: auto;
  border: 1px solid var(--pivot-shell-border);
  border-radius: 10px;
  background: var(--pivot-shell-bg);
  height: clamp(560px, calc(100vh - 340px), 720px);
}

.pivot-shell--mobile {
  height: min(58vh, 520px);
}

.pivot-shell__scroller {
  min-width: 840px;
  height: 100%;
}

.pivot-summary-card {
  border: 1px solid rgba(var(--v-theme-on-surface), 0.12);
}

.pivot-summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
  gap: 12px;
}

.pivot-element {
  display: block;
  width: 100%;
  height: 100%;
  min-width: 840px;
  min-height: 560px;
}

@media (max-width: 600px) {
  .pivot-shell__scroller {
    min-width: 760px;
  }

  .pivot-element {
    min-width: 760px;
    min-height: 500px;
  }
}
</style>

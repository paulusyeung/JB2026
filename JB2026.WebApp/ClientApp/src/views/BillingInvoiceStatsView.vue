<template>
  <section class="page-section billing-invoice-stats-page">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-text>
        <div class="toolbar-bar mb-3">
          <div class="view-heading">
            <h1 class="text-h6 font-weight-medium" role="heading" aria-level="1">{{ t('billing.invoiceStats.title') }}</h1>
            <div class="text-caption text-medium-emphasis">{{ t('billing.invoiceStats.subtitle') }}</div>
          </div>

          <div class="toolbar-filters">
            <v-menu v-model="startDatePickerOpen" :close-on-content-click="false">
              <template #activator="{ props: menuProps }">
                <v-text-field
                  :model-value="startDate ? format(startDate) : ''"
                  :label="t('billing.invoiceStats.startDate')"
                  variant="solo-filled"
                  density="comfortable"
                  readonly
                  append-inner-icon="mdi-calendar"
                  v-bind="menuProps"
                  hide-details
                  clearable
                  @click:clear="startDate = ''"
                />
              </template>
              <v-date-picker
                :model-value="startDate ? new Date(startDate + 'T12:00:00') : undefined"
                hide-header
                @update:model-value="onStartDatePicked"
              />
            </v-menu>

            <v-menu v-model="endDatePickerOpen" :close-on-content-click="false">
              <template #activator="{ props: menuProps }">
                <v-text-field
                  :model-value="endDate ? format(endDate) : ''"
                  :label="t('billing.invoiceStats.endDate')"
                  variant="solo-filled"
                  density="comfortable"
                  readonly
                  append-inner-icon="mdi-calendar"
                  v-bind="menuProps"
                  hide-details
                  clearable
                  @click:clear="endDate = ''"
                />
              </template>
              <v-date-picker
                :model-value="endDate ? new Date(endDate + 'T12:00:00') : undefined"
                hide-header
                @update:model-value="onEndDatePicked"
              />
            </v-menu>

            <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loading" @click="refresh">
              {{ t('common.search') }}
            </v-btn>
          </div>

          <div class="toolbar-actions">
            <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="load">
              {{ t('common.refresh') }}
            </v-btn>

            <v-btn
              variant="outlined"
              prepend-icon="mdi-microsoft-excel"
              :disabled="rows.length === 0"
              @click="exportToCsv"
            >
              {{ t('billing.invoiceStats.exportToExcel') }}
            </v-btn>
          </div>
        </div>

        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mt-3 mb-2">{{ errorMessage }}</v-alert>

        <v-alert
          v-if="isNarrowPhoneLayout"
          type="info"
          variant="tonal"
          density="compact"
          class="mt-2 mb-3"
        >
          {{ t('billing.invoiceStats.mobilePreferredNotice') }}
        </v-alert>

        <div class="text-caption text-medium-emphasis mt-2 mb-2">
          {{ t('billing.invoiceStats.rows', { count: formatNumber(rows.length) }) }}
        </div>

        

        <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-3" />

        <v-list v-if="isPhoneLayout && !loading && rows.length > 0" lines="two" class="invoice-list mb-4">
          <v-list-item v-for="row in sortedRows" :key="row.externalInvoiceId">
            <template #title>
              <div class="d-flex justify-space-between align-center gap-3">
                <span class="font-weight-medium">{{ row.customerName }}</span>
                <span class="text-primary font-weight-medium">{{ formatAmountCurrency(row.invoiceAmount) }}</span>
              </div>
            </template>
            <template #subtitle>
              <div class="d-flex flex-column text-caption">
                <span>{{ row.invoiceNumber }}</span>
                <span>{{ row.invoiceDate }}</span>
              </div>
            </template>
          </v-list-item>
        </v-list>

        <div v-if="!isPhoneLayout && pivotMounted && rows.length > 0" class="pivot-shell">
          <div class="pivot-shell__scroller">
            <web-pivot-table ref="pivotRef" class="pivot-element" />
          </div>
        </div>

        <div v-else-if="!pivotAvailable" class="text-body-2 text-medium-emphasis py-6 text-center">
          {{ t('billing.invoiceStats.loadFailed') }}
        </div>

        <div v-if="!loading && rows.length === 0" class="text-body-2 text-medium-emphasis py-6 text-center">
          {{ emptyMessage }}
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
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { listInvoices, type InvoiceBillingSummary } from '@/services/billing'
import { useThemeStore } from '@/stores/theme'

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

type BillingInvoiceStatsRow = {
  externalInvoiceId: string
  customerName: string
  invoiceNumber: string
  invoiceDate: string
  invoiceAmount: number
  year: string
  month: string
}

const { t } = useI18n({ useScope: 'global' })
const { locale } = useI18n({ useScope: 'global' })
const { formatCurrency, formatDate, formatNumber } = useLocaleFormatters()
const { format } = useGlobalDateFormatter()
const display = useDisplay()
const themeStore = useThemeStore()
const isPhoneLayout = computed(() => display.smAndDown.value)
const isNarrowPhoneLayout = computed(() => display.xs.value && display.width.value <= 430)
const currentYear = new Date().getFullYear()

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

const rows = ref<BillingInvoiceStatsRow[]>([])
const loading = ref(false)
const errorMessage = ref('')
const pivotRef = ref<WptElement | null>(null)
const pivotMounted = ref(false)
const pivotAvailable = ref(false)
const startDate = ref('')
const endDate = ref('')
const startDatePickerOpen = ref(false)
const endDatePickerOpen = ref(false)

let hydrateRetryTimer: number | null = null
let hydrateAttempts = 0
const MAX_HYDRATE_ATTEMPTS = 8

function toIsoDate(date: Date): string {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

function onStartDatePicked(date: Date | null) {
  if (date) {
    startDate.value = toIsoDate(date)
  }
  startDatePickerOpen.value = false
}

function onEndDatePicked(date: Date | null) {
  if (date) {
    endDate.value = toIsoDate(date)
  }
  endDatePickerOpen.value = false
}

const hasDateRangeFilter = computed(() => startDate.value.length > 0 || endDate.value.length > 0)
const emptyMessage = computed(() => (
  hasDateRangeFilter.value
    ? t('billing.invoiceStats.emptyFiltered')
    : t('billing.invoiceStats.empty')
))
const totalInvoiceAmount = computed(() => rows.value.reduce((total, row) => total + row.invoiceAmount, 0))
const uniqueInvoiceCount = computed(() => rows.value.length)
const sortedRows = computed(() => [...rows.value].sort((left, right) => right.invoiceAmount - left.invoiceAmount))

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
    errorMessage.value = t('billing.invoiceStats.loadFailed')
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
    const invoices = await listInvoices()
    rows.value = invoices
      .filter(matchesInvoiceFilter)
      .map(mapInvoiceToStatsRow)

    if (pivotMounted.value) {
      await nextTick()
      hydrateAttempts = 0
      scheduleHydratePivot()
    }
  } catch {
    errorMessage.value = t('billing.invoiceStats.loadFailed')
  } finally {
    loading.value = false
  }
}

async function refresh() {
  await load()
}

function matchesInvoiceFilter(invoice: InvoiceBillingSummary): boolean {
  const status = invoice.status?.trim().toLowerCase()
  if (status !== 'sent') {
    return false
  }

  const parsedDate = parseInvoiceDate(invoice.invoiceDate)
  if (!parsedDate || Number.isNaN(parsedDate.getTime())) {
    return false
  }

  if (!hasDateRangeFilter.value) {
    return parsedDate.getFullYear() === currentYear
  }

  const start = parseDateBoundary(startDate.value, 'start')
  if (start && parsedDate < start) {
    return false
  }

  const end = parseDateBoundary(endDate.value, 'end')
  if (end && parsedDate > end) {
    return false
  }

  return true
}

function mapInvoiceToStatsRow(invoice: InvoiceBillingSummary): BillingInvoiceStatsRow {
  const parsedDate = parseInvoiceDate(invoice.invoiceDate)
  const hasValidDate = Boolean(parsedDate && !Number.isNaN(parsedDate.getTime()))
  const unknownPeriod = t('billing.invoiceStats.unknownPeriod')

  return {
    externalInvoiceId: invoice.externalInvoiceId,
    customerName: invoice.clientName?.trim() || t('billing.invoiceStats.blank'),
    invoiceNumber: invoice.invoiceNumber?.trim() || invoice.externalInvoiceId,
    invoiceDate: hasValidDate && parsedDate ? formatDate(parsedDate) : unknownPeriod,
    invoiceAmount: Number(invoice.amount ?? 0),
    year: hasValidDate && parsedDate ? String(parsedDate.getFullYear()) : unknownPeriod,
    month: hasValidDate && parsedDate ? String(parsedDate.getMonth() + 1).padStart(2, '0') : unknownPeriod,
  }
}

function parseInvoiceDate(value?: string): Date | null {
  if (!value) {
    return null
  }

  const parsedDate = new Date(value)
  return Number.isNaN(parsedDate.getTime()) ? null : parsedDate
}

function parseDateBoundary(value: string, boundary: 'start' | 'end'): Date | null {
  if (!value) {
    return null
  }

  const parsedDate = new Date(`${value}T00:00:00`)
  if (Number.isNaN(parsedDate.getTime())) {
    return null
  }

  if (boundary === 'end') {
    parsedDate.setHours(23, 59, 59, 999)
  }

  return parsedDate
}

async function hydratePivot() {
  const pivot = pivotRef.value
  if (!pivot || rows.value.length === 0) {
    return false
  }

  const attrArray = ['CustomerName', 'InvoiceNumber', 'InvoiceDate', 'InvoiceAmount', 'Year', 'Month']
  const dataArray = rows.value.map((row) => [
    row.customerName,
    row.invoiceNumber,
    row.invoiceDate,
    row.invoiceAmount,
    row.year,
    row.month,
  ])

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

    await pivot.setWptFromDataArray?.(attrArray, dataArray, 'memory://billing-invoice-stats')

    const source = pivot.getSourceObject?.()
    if (source && source.mode === 'MEMORY' && (!source.url || typeof source.url !== 'string')) {
      pivot.setSourceObject?.({
        ...source,
        url: 'memory://billing-invoice-stats',
      })
    }

    pivot.configurePivot?.({
      displayMode: 'grid',
      rows: ['CustomerName'],
      columns: ['Year', 'Month'],
      values: [{
        field: 'InvoiceAmount',
        aggregation: 'SUM',
        format: {
          category: 'CURRENCY',
          decimal: 2,
          separatorFlag: true,
          symbol: '$',
          symbolSuffix: false,
        },
        formatter: (value: unknown) => {
          if (typeof value !== 'number') {
            return String(value)
          }

          return formatAmountCurrency(value)
        },
      }],
      showRowTotals: true,
      showColTotals: true,
    })

    return !hasNoStoreError(pivot)
  } catch {
    errorMessage.value = t('billing.invoiceStats.loadFailed')
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
  if (!pivot || typeof pivot.setWptFromDataArray !== 'function') {
    if (hydrateAttempts >= MAX_HYDRATE_ATTEMPTS) {
      errorMessage.value = t('billing.invoiceStats.loadFailed')
      return
    }

    hydrateAttempts += 1
    hydrateRetryTimer = window.setTimeout(scheduleHydratePivot, 250)
    return
  }

  const healthy = await hydratePivot()
  if (healthy) {
    return
  }

  if (hydrateAttempts >= MAX_HYDRATE_ATTEMPTS) {
    errorMessage.value = t('billing.invoiceStats.loadFailed')
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

function exportToCsv() {
  const header = ['CustomerName', 'InvoiceNumber', 'InvoiceDate', 'InvoiceAmount', 'Year', 'Month']
  const lines = [header.map(csvEscape).join(',')]

  for (const row of rows.value) {
    lines.push(
      [
        row.customerName,
        row.invoiceNumber,
        row.invoiceDate,
        formatNumber(row.invoiceAmount, { minimumFractionDigits: 2, maximumFractionDigits: 2 }),
        row.year,
        row.month,
      ]
        .map((value) => csvEscape(value ?? ''))
        .join(','),
    )
  }

  const blob = new Blob([`\uFEFF${lines.join('\n')}`], { type: 'text/csv;charset=utf-8;' })
  const link = document.createElement('a')
  const timestamp = new Date().toISOString().replace(/[-:T]/g, '').slice(0, 12)

  link.href = URL.createObjectURL(blob)
  link.download = `BillingInvoiceStats_${timestamp}.csv`
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(link.href)
}

function formatAmountCurrency(value: number): string {
  return `\$${formatNumber(value, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

function csvEscape(value: string | number): string {
  const escaped = String(value).replace(/"/g, '""')
  return `"${escaped}"`
}
</script>

<style scoped>
.billing-invoice-stats-page {
  --pivot-shell-border: rgba(var(--v-theme-on-surface), 0.2);
  --pivot-shell-bg: rgb(var(--v-theme-surface));
}

.toolbar-bar {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;
  flex-wrap: wrap;
}

.view-heading {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.toolbar-actions {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.toolbar-filters {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 12px;
  align-items: center;
  flex: 1 1 420px;
}

.pivot-shell {
  overflow: auto;
  border: 1px solid var(--pivot-shell-border);
  border-radius: 10px;
  background: var(--pivot-shell-bg);
  height: clamp(560px, calc(100vh - 340px), 720px);
}

.pivot-shell__scroller {
  min-width: 840px;
  height: 100%;
}

.pivot-element {
  display: block;
  width: 100%;
  height: 100%;
  min-width: 840px;
  min-height: 560px;
}

.invoice-list {
  border: 1px solid rgba(var(--v-theme-on-surface), 0.12);
  border-radius: 12px;
}

@media (max-width: 600px) {
  .toolbar-filters,
  .toolbar-actions {
    width: 100%;
  }

  .pivot-shell__scroller {
    min-width: 760px;
  }

  .pivot-element {
    min-width: 760px;
    min-height: 500px;
  }
}
</style>
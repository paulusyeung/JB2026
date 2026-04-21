<template>
  <section class="page-section sml-invoice-stats-page">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex flex-wrap align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('sml.invoiceStats.title') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('sml.invoiceStats.subtitle') }}</p>
        </div>
      </v-card-title>

      <v-card-text>
        <div class="filter-bar">
          <v-text-field
            v-model="lookup"
            density="comfortable"
            :label="t('sml.invoiceStats.lookup')"
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
            :label="t('sml.invoiceStats.startDate')"
            variant="solo-filled"
            hide-details
          />

          <v-text-field
            v-model="endOn"
            type="date"
            density="comfortable"
            :label="t('sml.invoiceStats.endDate')"
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
            :disabled="rows.length === 0"
            @click="exportToCsv"
          >
            {{ t('sml.invoiceStats.exportToExcel') }}
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
          {{ t('sml.invoiceStats.mobilePreferredNotice') }}
        </v-alert>

        <div class="text-caption text-medium-emphasis mt-2 mb-2">
          {{ t('sml.invoiceStats.rows', { count: formatNumber(rows.length) }) }}
        </div>

        <v-card v-if="isPhoneLayout" rounded="lg" variant="tonal" class="pivot-summary-card mb-3">
          <v-card-text>
            <div class="text-overline text-medium-emphasis mb-2">{{ t('sml.invoiceStats.summary.title') }}</div>
            <div class="pivot-summary-grid">
              <div>
                <div class="text-caption text-medium-emphasis">{{ t('sml.invoiceStats.summary.invoices') }}</div>
                <div class="text-body-2 font-weight-medium">{{ formatNumber(uniqueInvoiceCount) }}</div>
              </div>
              <div>
                <div class="text-caption text-medium-emphasis">{{ t('sml.invoiceStats.summary.rows') }}</div>
                <div class="text-body-2 font-weight-medium">{{ formatNumber(rows.length) }}</div>
              </div>
              <div>
                <div class="text-caption text-medium-emphasis">{{ t('sml.invoiceStats.summary.invoiceAmount') }}</div>
                <div class="text-body-2 font-weight-medium">{{ formatSummaryCurrency(totalInvoiceAmount) }}</div>
              </div>
              <div>
                <div class="text-caption text-medium-emphasis">{{ t('sml.invoiceStats.summary.pivotAmount') }}</div>
                <div class="text-body-2 font-weight-medium">{{ formatSummaryCurrency(totalPivotAmount) }}</div>
              </div>
              <div>
                <div class="text-caption text-medium-emphasis">{{ t('sml.invoiceStats.summary.qty') }}</div>
                <div class="text-body-2 font-weight-medium">{{ formatNumber(totalQty, { minimumFractionDigits: 0, maximumFractionDigits: 0 }) }}</div>
              </div>
            </div>
          </v-card-text>
        </v-card>

        <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-3" />

        <div v-if="pivotMounted" :class="['pivot-shell', { 'pivot-shell--mobile': isPhoneLayout }]">
          <div class="pivot-shell__scroller">
            <web-pivot-table ref="pivotRef" class="pivot-element" />
          </div>
        </div>

        <div v-else-if="!pivotAvailable" class="text-body-2 text-medium-emphasis py-6 text-center">
          {{ t('sml.invoiceStats.loadFailed') }}
        </div>

        <div v-if="!loading && rows.length === 0" class="text-body-2 text-medium-emphasis py-6 text-center">
            {{ t('sml.invoiceStats.empty') }}
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
import { getSmlInvoiceStats } from '@/services/sml'
import { useThemeStore } from '@/stores/theme'
import type { SmlInvoiceStatsRow } from '@/types/api'

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

const { t } = useI18n({ useScope: 'global' })
const { locale } = useI18n({ useScope: 'global' })
const { formatNumber } = useLocaleFormatters()
const display = useDisplay()
const themeStore = useThemeStore()
const isPhoneLayout = computed(() => display.smAndDown.value)
const isNarrowPhoneLayout = computed(() => display.xs.value && display.width.value <= 430)
const webPivotTheme = computed(() => {
  if (themeStore.current !== 'dark') {
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

const rows = ref<SmlInvoiceStatsRow[]>([])
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

const totalInvoiceAmount = computed(() => rows.value.reduce((total, row) => total + Number(row.invoiceAmount ?? 0), 0))
const totalPivotAmount = computed(() => rows.value.reduce((total, row) => total + Number(row.amount ?? 0), 0))
const totalQty = computed(() => rows.value.reduce((total, row) => total + Number(row.qty ?? 0), 0))
const uniqueInvoiceCount = computed(() => {
  const set = new Set<string>()
  for (const row of rows.value) {
    const invoice = row.invoiceNumber?.trim()
    if (invoice) {
      set.add(invoice)
    }
  }
  return set.size
})

watch(() => themeStore.current, async () => {
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
    errorMessage.value = t('sml.invoiceStats.loadFailed')
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
    const response = await getSmlInvoiceStats({
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
    errorMessage.value = t('sml.invoiceStats.loadFailed')
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

  const attrArray = ['CustomerName', 'InvoiceNumber', 'PurchaseOrder', 'ProductCode', 'Qty', 'Unit', 'Price', 'Year', 'Month', 'Amount']
  const dataArray = rows.value.map((row) => [
    row.customerName || t('sml.invoiceStats.blank'),
    row.invoiceNumber || t('sml.invoiceStats.blank'),
    row.purchaseOrder || t('sml.invoiceStats.blank'),
    row.productCode || t('sml.invoiceStats.blank'),
    Number(row.qty ?? 0),
    row.unit || '-',
    Number(row.price ?? 0),
    Number(row.year ?? 0),
    Number(row.month ?? 0),
    Number(row.amount ?? 0),
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

    await pivot.setWptFromDataArray?.(attrArray, dataArray, 'memory://sml-invoice-stats')

    const source = pivot.getSourceObject?.()
    if (source && source.mode === 'MEMORY' && (!source.url || typeof source.url !== 'string')) {
      pivot.setSourceObject?.({
        ...source,
        url: 'memory://sml-invoice-stats',
      })
    }

    pivot.configurePivot?.({
      displayMode: 'grid',
      rows: ['CustomerName', 'InvoiceNumber', 'PurchaseOrder', 'ProductCode'],
      columns: ['Year', 'Month'],
      values: [{
        field: 'Amount',
        aggregation: 'SUM',
        format: {
          category: 'CURRENCY',
          decimal: 2,
          separatorFlag: true,
          symbol: '$',
          symbolSuffix: false,
        },
        formatter: (value: unknown) => {
          if (typeof value !== 'number') return String(value)
          return formatAmountCurrency(value)
        },
      }],
      showRowTotals: true,
      showColTotals: true,
    })
    return !hasNoStoreError(pivot)
  } catch {
    errorMessage.value = t('sml.invoiceStats.loadFailed')
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
      errorMessage.value = t('sml.invoiceStats.loadFailed')
      return
    }

    hydrateAttempts += 1
    hydrateRetryTimer = window.setTimeout(scheduleHydratePivot, 250)
    return
  }

  const hasMethods = typeof pivot.setWptFromDataArray === 'function'
  if (!hasMethods) {
    if (hydrateAttempts >= MAX_HYDRATE_ATTEMPTS) {
      errorMessage.value = t('sml.invoiceStats.loadFailed')
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
    errorMessage.value = t('sml.invoiceStats.loadFailed')
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
  const header = [
    'CustomerName',
    'InvoiceNumber',
    'PurchaseOrder',
    'ProductCode',
    'Qty',
    'Unit',
    'Price',
    'Year',
    'Month',
    'Amount',
  ]

  const lines = [header.map(csvEscape).join(',')]

  for (const row of rows.value) {
    lines.push(
      [
        row.customerName,
        row.invoiceNumber,
        row.purchaseOrder,
        row.productCode,
        formatNumber(row.qty, { minimumFractionDigits: 0, maximumFractionDigits: 4 }),
        row.unit,
        formatNumber(row.price, { minimumFractionDigits: 2, maximumFractionDigits: 2 }),
        String(row.year),
        String(row.month),
        formatAmountCurrency(row.amount),
      ]
        .map((value) => csvEscape(value ?? ''))
        .join(','),
    )
  }

  const blob = new Blob([`\uFEFF${lines.join('\n')}`], { type: 'text/csv;charset=utf-8;' })
  const link = document.createElement('a')
  const timestamp = new Date().toISOString().replace(/[-:T]/g, '').slice(0, 12)

  link.href = URL.createObjectURL(blob)
  link.download = `InvoiceStats_${timestamp}.csv`
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(link.href)
}

function formatAmountCurrency(value: number): string {
  return value.toLocaleString('en-US', {
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

function csvEscape(value: string): string {
  const escaped = String(value).replace(/"/g, '""')
  return `"${escaped}"`
}
</script>

<style scoped>
.sml-invoice-stats-page {
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
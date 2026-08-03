<template>
  <section class="visualization-page" :class="{ 'is-dragging': isDragging }">
    <div class="resize-overlay" v-if="isDragging" @mousemove="onMouseMove" @mouseup="stopResize" />

    <div class="left-pane">
      <v-card rounded="xl" elevation="0" class="panel-card filter-card">
        <v-card-text>
          <div class="filter-section">
            <v-menu v-model="startDatePickerOpen" :close-on-content-click="false">
              <template #activator="{ props: menuProps }">
                <v-text-field
                  :model-value="startOn ? format(startOn) : ''"
                  :label="t('visualization.filters.invDateFrom')"
                  variant="solo-filled"
                  density="comfortable"
                  readonly
                  append-inner-icon="mdi-calendar"
                  v-bind="menuProps"
                  hide-details
                  clearable
                  @click:clear="startOn = ''"
                />
              </template>
              <v-date-picker
                :model-value="startOn ? new Date(startOn + 'T12:00:00') : undefined"
                hide-header
                @update:model-value="onStartDatePicked"
              />
            </v-menu>

            <v-menu v-model="endDatePickerOpen" :close-on-content-click="false">
              <template #activator="{ props: menuProps }">
                <v-text-field
                  :model-value="endOn ? format(endOn) : ''"
                  :label="t('visualization.filters.invDateTo')"
                  variant="solo-filled"
                  density="comfortable"
                  readonly
                  append-inner-icon="mdi-calendar"
                  v-bind="menuProps"
                  hide-details
                  clearable
                  @click:clear="endOn = ''"
                />
              </template>
              <v-date-picker
                :model-value="endOn ? new Date(endOn + 'T12:00:00') : undefined"
                hide-header
                @update:model-value="onEndDatePicked"
              />
            </v-menu>

            <v-select
              v-model="optionField"
              :items="optionItems"
              item-title="label"
              item-value="value"
              density="comfortable"
              :label="t('visualization.filters.options')"
              variant="solo-filled"
              hide-details
            />

            <v-select
              v-model="groupFilter"
              :items="groupItems"
              item-title="label"
              item-value="value"
              density="comfortable"
              :label="t('visualization.filters.group')"
              variant="solo-filled"
              hide-details
            />

            <div class="graph-types-label">{{ t('visualization.filters.graphTypes') }}</div>
            <v-btn-toggle v-model="graphType" mandatory divided density="compact" color="primary" variant="outlined" class="graph-types-toggle">
              <v-btn value="bell" icon>
                <v-icon>mdi-chart-bell-curve</v-icon>
                <v-tooltip activator="parent" location="top">{{ t('visualization.filters.graphTypeBell') }}</v-tooltip>
              </v-btn>
              <v-btn value="line" icon>
                <v-icon>mdi-chart-line</v-icon>
                <v-tooltip activator="parent" location="top">{{ t('visualization.filters.graphTypeLine') }}</v-tooltip>
              </v-btn>
              <v-btn value="stack" icon>
                <v-icon>mdi-chart-bar-stacked</v-icon>
                <v-tooltip activator="parent" location="top">{{ t('visualization.filters.graphTypeStack') }}</v-tooltip>
              </v-btn>
              <v-btn value="diverging" icon>
                <v-icon>mdi-chart-gantt</v-icon>
                <v-tooltip activator="parent" location="top">{{ t('visualization.filters.graphTypeDiverging') }}</v-tooltip>
              </v-btn>
            </v-btn-toggle>

            <v-divider class="my-2" />

            <div class="graph-size-label">{{ t('visualization.graphSize') }}</div>
            <v-slider
              v-model="graphScale"
              :min="0.5"
              :max="3"
              :step="0.1"
              density="compact"
              hide-details
              class="mx-1"
            >
              <template #append>
                <span class="text-caption text-medium-emphasis" style="min-width:2.2em">{{ graphScale.toFixed(1) }}x</span>
              </template>
            </v-slider>

            <v-divider class="my-2" />

            <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loading" @click="refresh" block class="mb-2">
              {{ t('visualization.search') }}
            </v-btn>

            <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="load" block class="mb-2">
              {{ t('visualization.refresh') }}
            </v-btn>

            <v-btn
              variant="outlined"
              prepend-icon="mdi-microsoft-excel"
              :disabled="filteredRows.length === 0"
              @click="exportToCsv"
              block
            >
              {{ t('visualization.exportToExcel') }}
            </v-btn>
          </div>
        </v-card-text>
      </v-card>
    </div>

    <div class="splitter" @mousedown="startResize" />

    <div class="right-pane">
      <v-card rounded="xl" elevation="0" class="panel-card result-card">
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="ma-4 mb-0">{{ errorMessage }}</v-alert>

        <v-card-text>
          <div class="result-toolbar">
            <div class="text-caption text-medium-emphasis">
              {{ t('visualization.rows', { count: formatNumber(filteredRows.length) }) }}
            </div>
          </div>

          <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-3" />

          <div v-if="!loading && filteredRows.length === 0" class="text-body-2 text-medium-emphasis py-8 text-center">
            {{ t('visualization.empty') }}
          </div>

          <div v-if="!loading && filteredRows.length > 0 && (graphType === 'bell' || graphType === 'line' || graphType === 'stack' || graphType === 'diverging')" ref="plotContainer" class="plot-container" />
        </v-card-text>
      </v-card>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { getJobStats } from '@/services/jobOrders'
import { useThemeStore } from '@/stores/theme'
import type { JobStatsRecord } from '@/types/api'
import * as Plot from '@observablehq/plot'

const STORAGE_KEY = 'visualization-left-pane-width'
const MIN_WIDTH_PX = 280
const MAX_WIDTH_PX = 500

const leftPaneWidth = ref(loadStoredWidth())
const isDragging = ref(false)
const themeStore = useThemeStore()

function loadStoredWidth(): number {
  const stored = localStorage.getItem(STORAGE_KEY)
  if (stored) {
    const parsed = parseFloat(stored)
    if (!isNaN(parsed)) return Math.max(MIN_WIDTH_PX, Math.min(MAX_WIDTH_PX, parsed))
  }
  return 320
}

function startResize(e: MouseEvent) {
  e.preventDefault()
  isDragging.value = true
}

function onMouseMove(e: MouseEvent) {
  const clamped = Math.max(MIN_WIDTH_PX, Math.min(MAX_WIDTH_PX, e.clientX))
  leftPaneWidth.value = clamped
  document.documentElement.style.setProperty('--viz-left-pane-width', clamped + 'px')
}

function stopResize() {
  isDragging.value = false
  localStorage.setItem(STORAGE_KEY, String(leftPaneWidth.value))
}

const { t } = useI18n({ useScope: 'global' })
const { format } = useGlobalDateFormatter()
const { formatNumber } = useLocaleFormatters()

const rows = ref<JobStatsRecord[]>([])
const loading = ref(false)
const errorMessage = ref('')
const plotContainer = ref<HTMLElement | null>(null)
let plotSvg: Element | null = null

function defaultStartOn(): string {
  const d = new Date()
  d.setMonth(d.getMonth() - 12)
  return d.toISOString().slice(0, 10)
}

function defaultEndOn(): string {
  return new Date().toISOString().slice(0, 10)
}

const startOn = ref(defaultStartOn())
const endOn = ref(defaultEndOn())
const startDatePickerOpen = ref(false)
const endDatePickerOpen = ref(false)
const optionField = ref<'salesRep' | 'customer'>('salesRep')
const groupFilter = ref('none')
const graphType = ref<'bell' | 'line' | 'stack' | 'diverging'>('bell')
const graphScale = ref(1.5)

const optionItems = computed(() => [
  { value: 'salesRep', label: t('visualization.filters.optionsSalesRep') },
  { value: 'customer', label: t('visualization.filters.optionsCustomer') },
])

const groupItems = computed(() => [
  { value: 'none', label: t('visualization.filters.groupNone') },
  { value: 'top10', label: t('visualization.filters.groupTop10') },
  { value: 'top50', label: t('visualization.filters.groupTop50') },
  { value: 'bottom50', label: t('visualization.filters.groupBottom50') },
  { value: 'bottom10', label: t('visualization.filters.groupBottom10') },
])

const tableHeaders = computed(() => [
  { title: 'Job Number', key: 'jobNumber', sortable: true },
  { title: 'Customer Name', key: 'customerName', sortable: true },
  { title: 'Brand', key: 'brand', sortable: true },
  { title: 'Sales Rep', key: 'salesRep', sortable: true },
  { title: 'Invoice Amount', key: 'invoiceAmount', sortable: true },
  { title: 'Cost', key: 'cost', sortable: true },
  { title: 'Inv Number', key: 'invNumber', sortable: true },
  { title: 'Inv Date', key: 'invDate', sortable: true },
])

const filteredRows = computed(() => {
  return rows.value.filter((row) => isWithinLastTenYears(row))
})

const groupedData = computed(() => {
  const keyField = optionField.value === 'customer' ? 'customerName' : 'salesRep'
  const map = new Map<string, number>()
  for (const row of filteredRows.value) {
    const key = String(row[keyField as keyof JobStatsRecord] ?? '').trim()
    if (!key) continue
    map.set(key, (map.get(key) ?? 0) + Number(row.invoiceAmount ?? 0))
  }
  return Array.from(map.entries()).map(([name, total]) => ({ name, total }))
})

const filteredGroupedData = computed(() => {
  const data = groupedData.value
  const filter = groupFilter.value
  if (filter === 'none') return data
  const sorted = [...data].sort((a, b) => b.total - a.total)
  if (filter === 'top10') return sorted.slice(0, 10)
  if (filter === 'top50') return sorted.slice(0, 50)
  if (filter === 'bottom50') return sorted.slice(-50)
  if (filter === 'bottom10') return sorted.slice(-10)
  return data
})

const hasPlotData = computed(() => filteredGroupedData.value.length > 0)

async function renderPlot() {
  const container = plotContainer.value
  if (!container) return

  if (plotSvg && plotSvg.parentElement === container) {
    container.removeChild(plotSvg)
    plotSvg = null
  }
  container.querySelectorAll('.line-legend').forEach(el => el.remove())

  const isDark = themeStore.mode === 'dark'

  if (graphType.value === 'line') {
    errorMessage.value = ''

    const keyField = optionField.value === 'customer' ? 'customerName' : 'salesRep'
    const isCustomer = optionField.value === 'customer'
    const entityPlural = t(isCustomer ? 'visualization.bell.entityCustomerPlural' : 'visualization.bell.entityRepPlural')

    const monthRepMap = new Map<string, Map<string, number>>()
    const repTotals = new Map<string, number>()

    for (const row of filteredRows.value) {
      const name = String(row[keyField as keyof JobStatsRecord] ?? '').trim()
      if (!name) continue
      const year = normalizeToGregorianYear(row.year)
      if (!Number.isFinite(year)) continue
      const m = Number(row.month)
      if (!Number.isFinite(m) || m < 1 || m > 12) continue
      const monthKey = `${year}-${String(m).padStart(2, '0')}`

      let repMonths = monthRepMap.get(name)
      if (!repMonths) {
        repMonths = new Map()
        monthRepMap.set(name, repMonths)
      }
      repMonths.set(monthKey, (repMonths.get(monthKey) ?? 0) + Number(row.invoiceAmount ?? 0))
      repTotals.set(name, (repTotals.get(name) ?? 0) + Number(row.invoiceAmount ?? 0))
    }

    if (monthRepMap.size === 0) {
      errorMessage.value = t('visualization.line.empty')
      return
    }

    const sortedReps = Array.from(repTotals.entries()).sort((a, b) => b[1] - a[1])
    const filter = groupFilter.value
    let selectedReps: string[]
    if (filter === 'none') {
      selectedReps = sortedReps.map(([n]) => n)
    } else if (filter === 'top10') {
      selectedReps = sortedReps.slice(0, 10).map(([n]) => n)
    } else if (filter === 'top50') {
      selectedReps = sortedReps.slice(0, 50).map(([n]) => n)
    } else if (filter === 'bottom50') {
      selectedReps = sortedReps.slice(-50).map(([n]) => n)
    } else if (filter === 'bottom10') {
      selectedReps = sortedReps.slice(-10).map(([n]) => n)
    } else {
      selectedReps = sortedReps.map(([n]) => n)
    }

    const allMonthKeys = new Set<string>()
    for (const repName of selectedReps) {
      const months = monthRepMap.get(repName)
      if (months) for (const mk of months.keys()) allMonthKeys.add(mk)
    }
    const sortedMonths = Array.from(allMonthKeys).sort()

    const lineData: { date: Date; total: number; name: string }[] = []
    for (const repName of selectedReps) {
      const months = monthRepMap.get(repName)
      if (!months) continue
      for (const mk of sortedMonths) {
        const total = months.get(mk) ?? 0
        const [y, m] = mk.split('-').map(Number)
        lineData.push({ date: new Date(y, m - 1), total, name: repName })
      }
    }

    if (lineData.length === 0) {
      errorMessage.value = t('visualization.line.empty')
      return
    }

    const plotData = selectedReps.length > 20 ? selectedReps.slice(0, 20) : selectedReps
    const palette = ['#4269d0', '#efb118', '#ff725c', '#6cc5b0', '#3ca951', '#ff8ab7', '#a463f2', '#97bbf5', '#9c6b4e', '#9498a0', '#4c9a8a', '#e8a838', '#c4513a', '#41977e', '#2f7f35', '#d4678d', '#7d3ac0', '#6a8fd4', '#7a4d2e', '#6b7078']
    const colorMap = new Map(plotData.map((n, i) => [n, palette[i % palette.length]]))

    try {
      const plotEl = Plot.plot({
        title: t('visualization.chartTitles.line', { entity: entityPlural }),
        width: Math.round(640 * graphScale.value),
        height: Math.round(400 * graphScale.value),
        marks: [
          Plot.lineY(lineData, {
            x: 'date',
            y: 'total',
            stroke: 'name',
            strokeWidth: 2,
            strokeOpacity: 0.7,
          }),
          Plot.dot(lineData, {
            x: 'date',
            y: 'total',
            stroke: 'name',
            r: 2.5,
            title: (d: any) =>
              `${d.name}: $${(d.total ?? 0).toLocaleString('en-US', { minimumFractionDigits: 0 })} (${d.date.toLocaleDateString('en-US', { year: 'numeric', month: 'short' })})`,
          }),
          Plot.ruleY([0]),
        ],
        x: {
          label: t('visualization.line.xLabel') + ' →',
          tickFormat: (d: Date) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`,
        },
        y: {
          label: '↑ ' + t('visualization.line.yLabel'),
          tickFormat: (d: number) => {
            if (d >= 1_000_000) return `$${(d / 1_000_000).toFixed(1)}M`
            if (d >= 1_000) return `$${Math.round(d / 1_000)}K`
            return `$${d}`
          },
        },
        color: { domain: plotData, range: palette },
        marginLeft: 60,
        marginTop: 50,
        marginBottom: 50,
        style: {
          background: isDark ? '#1e241f' : '#ffffff',
          color: isDark ? '#d7ddd3' : '#333333',
          fontSize: '13px',
          fontFamily: 'system-ui, sans-serif',
          maxWidth: 'none',
        },
        insetTop: 20,
      })
      mountPlot(plotEl)

      const legendDiv = document.createElement('div')
      legendDiv.className = 'line-legend'
      for (const [name, color] of colorMap) {
        const item = document.createElement('span')
        item.className = 'line-legend-item'
        item.innerHTML = `<span class="swatch" style="background:${color}"></span>${name}`
        item.addEventListener('mouseenter', () => {
          plotEl.querySelectorAll('path').forEach((p: SVGPathElement) => {
            const s = p.getAttribute('stroke')
            if (s === color) {
              p.style.opacity = ''
            } else if (s && s.startsWith('#')) {
              p.style.opacity = '0.15'
            }
          })
        })
        item.addEventListener('mouseleave', () => {
          plotEl.querySelectorAll('path').forEach((p: SVGPathElement) => {
            p.style.opacity = ''
          })
        })
        legendDiv.appendChild(item)
      }
      if (legendDiv.children.length > 0) {
        container.appendChild(legendDiv)
      }
    } catch (err) {
      errorMessage.value = String(err)
    }
    return
  }

  if (graphType.value === 'stack') {
    errorMessage.value = ''

    const stackKeyField = optionField.value === 'customer' ? 'customerName' : 'salesRep'
    const includedNames = new Set(filteredGroupedData.value.map((d) => d.name))

    const monthMap = new Map<string, number>()
    for (const row of filteredRows.value) {
      const entityName = String(row[stackKeyField as keyof JobStatsRecord] ?? '').trim()
      if (!entityName || !includedNames.has(entityName)) continue
      const year = normalizeToGregorianYear(row.year)
      if (!Number.isFinite(year)) continue
      const m = Number(row.month)
      if (!Number.isFinite(m) || m < 1 || m > 12) continue
      const key = `${year}-${String(m).padStart(2, '0')}`
      monthMap.set(key, (monthMap.get(key) ?? 0) + Number(row.invoiceAmount ?? 0))
    }

    const allMonths = Array.from(monthMap.entries())
      .map(([mk, revenue]) => {
        const [y, mNum] = mk.split('-').map(Number)
        return { year: y, month: mNum, revenue }
      })
      .sort((a, b) => a.year - b.year || a.month - b.month)

    if (allMonths.length === 0) {
      errorMessage.value = t('visualization.stack.empty')
      return
    }

    const chartData = allMonths.map((d, i) => {
      const prev = i > 0 ? allMonths[i - 1].revenue : d.revenue
      const growth = prev > 0 ? ((d.revenue - prev) / prev) * 100 : 0
      const label = `${d.year}-${String(d.month).padStart(2, '0')}`
      return { month: label, revenue: d.revenue, growth: Math.round(growth * 10) / 10 }
    })

    const maxRev = Math.max(...chartData.map((d) => d.revenue), 1)
    const maxGrowth = Math.max(...chartData.map((d) => Math.abs(d.growth)), 1)
    const scaleFactor = maxRev / maxGrowth
    const scaledData = chartData.map((d) => ({ ...d, scaledGrowth: d.growth * scaleFactor }))

    const tickCount = 4
    const step = Math.ceil(maxGrowth / tickCount)
    const lastMonth = chartData.length > 0 ? chartData[chartData.length - 1].month : ''
    const rightTicks: { pos: number; label: string; xPos: string }[] = []
    for (let i = -tickCount; i <= tickCount; i++) {
      const v = i * step
      rightTicks.push({ pos: v * scaleFactor, label: `${v > 0 ? '+' : ''}${v}%`, xPos: lastMonth })
    }
    const maxTickPos = Math.max(...rightTicks.map((t) => t.pos))
    const titleY = maxTickPos * 1.15 || maxRev

    try {
      const plotEl = Plot.plot({
        title: t('visualization.chartTitles.stack'),
        width: Math.round(640 * graphScale.value),
        height: Math.round(400 * graphScale.value),
        marks: [
          Plot.barY(scaledData, {
            x: 'month',
            y: 'revenue',
            fill: isDark ? '#e29a60' : '#4f708f',
            fillOpacity: 0.6,
          }),
          Plot.text(scaledData, {
            x: 'month',
            y: 'revenue',
            text: (d: any) => {
              if (d.revenue >= 1_000_000) return `$${(d.revenue / 1_000_000).toFixed(1)}M`
              if (d.revenue >= 1_000) return `$${Math.round(d.revenue / 1_000)}K`
              return `$${d.revenue}`
            },
            dy: -6,
            textAnchor: 'middle',
            fill: isDark ? '#d7ddd3' : '#333333',
            fontSize: 11,
          }),
          Plot.line(scaledData, {
            x: 'month',
            y: 'scaledGrowth',
            stroke: isDark ? '#8cb9d4' : '#c0392b',
            strokeWidth: 2.5,
            sort: null,
          }),
          Plot.dot(scaledData, {
            x: 'month',
            y: 'scaledGrowth',
            stroke: isDark ? '#8cb9d4' : '#c0392b',
            fill: isDark ? '#1e241f' : '#ffffff',
            r: 4,
            sort: null,
            title: (d: any) => `${d.month}: ${d.growth}%`,
          }),
          Plot.text(scaledData, {
            x: 'month',
            y: 'scaledGrowth',
            text: (d: any) => `${d.growth > 0 ? '+' : ''}${d.growth}%`,
            dy: -10,
            textAnchor: 'middle',
            fill: isDark ? '#8cb9d4' : '#c0392b',
            fontSize: 11,
            fontWeight: 'bold',
          }),
          // Right axis tick labels
          Plot.text(rightTicks, {
            x: 'xPos',
            y: 'pos',
            text: 'label',
            dx: 12,
            textAnchor: 'start',
            fill: isDark ? '#8cb9d4' : '#c0392b',
            fontSize: 10,
          }),
          Plot.text(
            [{ label: t('visualization.stack.xLabelGrowth'), xPos: lastMonth }],
            {
              x: 'xPos',
              y: titleY,
              text: 'label',
              dx: 22,
              textAnchor: 'start',
              fill: isDark ? '#8cb9d4' : '#c0392b',
              fontSize: 10,
            },
          ),
          Plot.ruleY([0]),
        ],
        x: {
          label: t('visualization.stack.yLabel') + ' →',
          type: 'band',
          tickFormat: (d: string) => d,
        },
        y: {
          label: t('visualization.stack.xLabelRev') + ' ↑',
          grid: true,
          tickFormat: (d: number) => {
            const neg = d < 0 ? '-' : ''
            const abs = Math.abs(d)
            if (abs >= 1_000_000) return `${neg}$${(abs / 1_000_000).toFixed(1)}M`
            if (abs >= 1_000) return `${neg}$${Math.round(abs / 1_000)}K`
            return `${neg}$${abs}`
          },
        },
        marginLeft: 60,
        marginRight: 70,
        marginTop: 50,
        marginBottom: 50,
        style: {
          background: isDark ? '#1e241f' : '#ffffff',
          color: isDark ? '#d7ddd3' : '#333333',
          fontSize: '13px',
          fontFamily: 'system-ui, sans-serif',
          maxWidth: 'none',
        },
        insetTop: 20,
      })
      mountPlot(plotEl)
    } catch (err) {
      errorMessage.value = String(err)
    }
    return
  }

  if (graphType.value === 'diverging') {
    errorMessage.value = ''

    const data = filteredGroupedData.value
    if (data.length === 0) {
      errorMessage.value = t('visualization.diverging.empty')
      return
    }

    const isCustomer = optionField.value === 'customer'
    const entityPlural = t(isCustomer ? 'visualization.bell.entityCustomerPlural' : 'visualization.bell.entityRepPlural')

    const grandTotal = data.reduce((sum, d) => sum + d.total, 0)
    const average = grandTotal / data.length

    type DivergingRow = { name: string; total: number; change: number }

    const chartData: DivergingRow[] = data
      .map((d) => ({ name: d.name, total: d.total, change: average > 0 ? ((d.total - average) / average) * 100 : 0 }))
      .sort((a, b) => b.change - a.change)

    const maxAbs = Math.max(...chartData.map((d) => Math.abs(d.change)), 1)
    const xDomain = [-maxAbs * 1.15, maxAbs * 1.15]

    const barHeight = 12
    const height = Math.round(Math.max(220, chartData.length * barHeight + 90) * graphScale.value)

    const positiveColor = '#6cc5b0'
    const negativeColor = '#ff725c'

    const pctTick = (d: number) => `${Math.round(d * 10) / 10}%`

    const formatPct = (v: number) => `${v >= 0 ? '+' : ''}${Math.round(Math.abs(v) * 10) / 10}%`

    const formatMoney = (v: number) => {
      if (v >= 1_000_000) return `$${(v / 1_000_000).toFixed(1)}M`
      if (v >= 1_000) return `$${Math.round(v / 1_000)}K`
      return `$${Math.round(v)}`
    }

    try {
      const plotEl = Plot.plot({
        title: t('visualization.chartTitles.diverging', { entity: entityPlural }),
        width: Math.round(640 * graphScale.value),
        height,
        marks: [
          Plot.gridX({ stroke: '#e0e0e0', strokeOpacity: 1 }),
          Plot.gridY({ stroke: '#e0e0e0', strokeOpacity: 1 }),
          Plot.barX(chartData, {
            x: 'change',
            y: 'name',
            fill: (d: DivergingRow) => (d.change >= 0 ? positiveColor : negativeColor),
            fillOpacity: 0.85,
            sort: null,
            title: (d: DivergingRow) =>
              `${d.name}\n${t('visualization.diverging.totalLabel')}: $${d.total.toLocaleString('en-US')}\n${t('visualization.diverging.averageLabel')}: $${average.toLocaleString('en-US')}\n${t('visualization.diverging.changeLabel')}: ${formatPct(d.change)}`,
          }),
          Plot.ruleX([0], { stroke: '#999999', strokeOpacity: 0.9, strokeWidth: 1.5 }),
          Plot.text(chartData.filter((d) => d.change >= 0), {
            x: 0,
            y: 'name',
            text: 'name',
            fill: '#333333',
            fontSize: 13,
            textAnchor: 'end',
            dx: -8,
            sort: null,
          }),
          Plot.text(chartData.filter((d) => d.change < 0), {
            x: 0,
            y: 'name',
            text: 'name',
            fill: '#333333',
            fontSize: 13,
            textAnchor: 'start',
            dx: 8,
            sort: null,
          }),
          Plot.text(chartData.filter((d) => d.change >= 0), {
            x: 'change',
            y: 'name',
            text: (d: DivergingRow) => formatPct(d.change),
            fill: positiveColor,
            fontSize: 13,
            textAnchor: 'start',
            dx: 6,
            sort: null,
          }),
          Plot.text(chartData.filter((d) => d.change < 0), {
            x: 'change',
            y: 'name',
            text: (d: DivergingRow) => formatPct(d.change),
            fill: negativeColor,
            fontSize: 13,
            textAnchor: 'end',
            dx: -6,
            sort: null,
          }),
          Plot.text([`(${formatMoney(average)})`], {
            x: 0,
            frameAnchor: 'bottom',
            dy: 34,
            textAnchor: 'middle',
            fill: '#333333',
            fontSize: 13,
          }),
        ],
        x: {
          domain: xDomain,
          label: t('visualization.diverging.xLabel') + ' →',
          tickFormat: pctTick,
        },
        y: {
          domain: chartData.map((d) => d.name),
          axis: null,
        },
        marginLeft: 20,
        marginRight: 20,
        marginTop: 50,
        marginBottom: 60,
        style: {
          background: '#ffffff',
          color: '#333333',
          fontSize: '13px',
          fontFamily: 'system-ui, sans-serif',
          maxWidth: 'none',
        },
        insetTop: 10,
        insetBottom: 10,
      })
      mountPlot(plotEl)
    } catch (err) {
      errorMessage.value = String(err)
    }
    return
  }

  if (graphType.value !== 'bell') return

  const data = filteredGroupedData.value
  const isCustomer = optionField.value === 'customer'
  const entityPlural = t(isCustomer ? 'visualization.bell.entityCustomerPlural' : 'visualization.bell.entityRepPlural')
  const entitySingular = t(isCustomer ? 'visualization.bell.entityCustomer' : 'visualization.bell.entityRep')

  if (data.length < 3) {
    errorMessage.value = t('visualization.bell.needMoreData', { entity: entityPlural })
    return
  }

  try {
    const totals = data.map((d) => d.total)
    const n = totals.length
    const min = Math.min(...totals)
    const max = Math.max(...totals)
    if (min === max) {
      errorMessage.value = t('visualization.bell.allSameTotal', { entity: entityPlural })
      return
    }

    const mean = totals.reduce((s, v) => s + v, 0) / n
    const variance = totals.reduce((s, v) => s + (v - mean) ** 2, 0) / n
    const std = Math.sqrt(variance)
    if (std === 0) {
      errorMessage.value = t('visualization.bell.noVariance')
      return
    }

    const normalPDF = (x: number) =>
      (1 / (std * Math.sqrt(2 * Math.PI))) * Math.exp(-0.5 * ((x - mean) / std) ** 2)

    const k = Math.max(1, Math.ceil(Math.log2(n) + 1))
    const binWidth = (max - min) / k
    const thresholds = Array.from({ length: k + 1 }, (_, i) => min + i * binWidth)

    const curveStart = Math.max(0, mean - 4 * std)
    const curveEnd = mean + 4 * std
    const curveStep = (curveEnd - curveStart) / 200
    const curveData: { x: number; y: number }[] = []
    for (let x = curveStart; x <= curveEnd; x += curveStep) {
      curveData.push({ x, y: normalPDF(x) * binWidth })
    }

    const histogramData: { x0: number; x1: number; proportion: number; count: number }[] = []
    for (let i = 0; i < k; i++) {
      const lo = thresholds[i]
      const hi = thresholds[i + 1]
      const count = totals.filter((t) => t >= lo && (i === k - 1 ? t <= hi : t < hi)).length
      histogramData.push({ x0: lo, x1: hi, proportion: count / n, count })
    }

    const dotData = data.map((d) => ({ name: d.name, total: d.total, y: normalPDF(d.total) * binWidth }))

    const isDark = themeStore.mode === 'dark'

    const plotEl = Plot.plot({
      title: t('visualization.chartTitles.bell', { entity: entityPlural }),
      width: Math.round(640 * graphScale.value),
      height: Math.round(400 * graphScale.value),
      marks: [
        Plot.rectY(histogramData, {
          x1: 'x0',
          x2: 'x1',
          y: 'proportion',
          fill: isDark ? '#e29a60' : '#4f708f',
          fillOpacity: 0.4,
          title: (d: any) =>
            `$${(d.x0 ?? 0).toLocaleString('en-US', { minimumFractionDigits: 0 })} – $${(d.x1 ?? 0).toLocaleString('en-US', { minimumFractionDigits: 0 })}\n${t('visualization.bell.countLabel', { count: d.count, entity: entityPlural })}`,
        }),
        Plot.areaY(curveData, {
          x: 'x',
          y: 'y',
          fill: isDark ? '#8cb9d4' : '#c0392b',
          fillOpacity: 0.12,
        }),
        Plot.lineY(curveData, {
          x: 'x',
          y: 'y',
          stroke: isDark ? '#8cb9d4' : '#c0392b',
          strokeWidth: 2.5,
        }),
        Plot.ruleX([mean], {
          stroke: 'orange',
          strokeWidth: 2,
          strokeDasharray: '4 4',
        }),
        Plot.dot(dotData, {
          x: 'total',
          y: 'y',
          fill: isDark ? '#d7ddd3' : '#333333',
          r: 4,
          channels: { [t('visualization.bell.channelName', { entity: entitySingular })]: 'name', [t('visualization.bell.channelTotal')]: 'total' },
          tip: true,
        }),
        Plot.ruleY([0]),
      ],
      x: {
        label: t('visualization.bell.xLabel') + ' →',
        nice: true,
        ticks: 8,
          tickFormat: (d: number) => {
            const neg = d < 0 ? '-' : ''
            const abs = Math.abs(d)
            if (abs >= 1_000_000) return `${neg}$${(abs / 1_000_000).toFixed(1)}M`
            if (abs >= 1_000) return `${neg}$${Math.round(abs / 1_000)}K`
            return `${neg}$${abs}`
          },
      },
      y: {
        label: '↑ ' + t('visualization.bell.yLabel', { entity: entityPlural }),
        nice: true,
        tickFormat: (d: number) => `${(d * 100).toFixed(1)}%`,
      },
      marginLeft: 60,
      marginRight: 40,
      marginTop: 50,
      marginBottom: 40,
      style: {
        background: isDark ? '#1e241f' : '#ffffff',
        color: isDark ? '#d7ddd3' : '#333333',
        fontSize: '13px',
        fontFamily: 'system-ui, sans-serif',
        maxWidth: 'none',
      },
      insetTop: 20,
    })

    errorMessage.value = ''

    mountPlot(plotEl)
  } catch (err) {
    errorMessage.value = String(err)
  }
}

function mountPlot(plotEl: Element) {
  const container = plotContainer.value
  if (!container) return
  container.appendChild(plotEl)
  stylePlotTitle(plotEl)
  plotSvg = plotEl
}

function stylePlotTitle(plotEl: Element) {
  const titleEl = plotEl.querySelector(':scope > h2')
  if (!titleEl) return
  const el = titleEl as HTMLElement
  const isDark = themeStore.mode === 'dark'
  el.style.textAlign = 'center'
  el.style.margin = '0 0 8px'
  el.style.fontSize = '14px'
  el.style.fontWeight = '600'
  el.style.color = isDark ? '#d7ddd3' : '#333333'
}

watch([graphType, themeStore, graphScale, optionField, groupFilter], async () => {
  await renderPlot()
})

async function load() {
  loading.value = true
  errorMessage.value = ''

  try {
    rows.value = await getJobStats({
      startOn: startOn.value || undefined,
      endOn: endOn.value || undefined,
    })
  } catch {
    errorMessage.value = t('visualization.loadFailed')
  } finally {
    loading.value = false
    await nextTick()
    await renderPlot()
  }
}

async function refresh() {
  await load()
}

function toIsoDate(date: Date): string {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

function onStartDatePicked(date: Date | null) {
  if (date) {
    startOn.value = toIsoDate(date)
  }
  startDatePickerOpen.value = false
}

function onEndDatePicked(date: Date | null) {
  if (date) {
    endOn.value = toIsoDate(date)
  }
  endDatePickerOpen.value = false
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
  if (!Number.isFinite(rawYear)) return Number.NaN

  const integerYear = Math.trunc(rawYear)

  if (integerYear > 0 && integerYear < 300) {
    return integerYear + 1911
  }

  return integerYear
}

function csvEscape(value: unknown): string {
  const escaped = String(value).replace(/"/g, '""')
  return `"${escaped}"`
}

function exportToCsv() {
  const header = [
    'Job Number',
    'Customer Name',
    'Brand',
    'Purchase Order',
    'Sales Rep',
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
        csvEscape(row.jobNumber),
        csvEscape(row.customerName),
        csvEscape(row.brand),
        csvEscape(row.purchaseOrder),
        csvEscape(row.salesRep),
        csvEscape(Number(row.cost ?? 0).toFixed(2)),
        csvEscape(Number(row.invoiceAmount ?? 0).toFixed(2)),
        csvEscape(row.invNumber),
        csvEscape(row.invDate),
        csvEscape(String(row.year ?? 0)),
        csvEscape(String(row.month ?? 0)),
      ].join(','),
    )
  }

  const blob = new Blob([`\uFEFF${lines.join('\n')}`], { type: 'text/csv;charset=utf-8;' })
  const link = document.createElement('a')
  const timestamp = new Date().toISOString().replace(/[-:T]/g, '').slice(0, 12)
  link.href = URL.createObjectURL(blob)
  link.download = `Visualization_${timestamp}.csv`
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(link.href)
}

onMounted(async () => {
  document.documentElement.style.setProperty('--viz-left-pane-width', leftPaneWidth.value + 'px')
  await nextTick()
  await renderPlot()
})

onUnmounted(() => {
  if (plotSvg && plotSvg.parentElement) {
    plotSvg.parentElement.removeChild(plotSvg)
  }
  plotSvg = null
})
</script>

<style scoped>
.visualization-page {
  display: flex;
  height: calc(100vh - 7rem);
  position: relative;
}

.visualization-page.is-dragging {
  cursor: col-resize;
  user-select: none;
}

.resize-overlay {
  position: fixed;
  inset: 0;
  z-index: 9999;
  cursor: col-resize;
}

.left-pane {
  width: var(--viz-left-pane-width, 320px);
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
}

.splitter {
  width: 4px;
  flex-shrink: 0;
  cursor: col-resize;
  background: transparent;
  transition: background 0.15s;
  margin: 0 2px;
  border-radius: 2px;
}

.splitter:hover,
.is-dragging .splitter {
  background: rgb(var(--v-theme-primary));
}

.right-pane {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.filter-card {
  flex: 1;
}

.result-card {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.result-card :deep(.v-card-text) {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.filter-section {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.graph-types-label,
.graph-size-label {
  font-size: 0.875rem;
  color: rgba(var(--v-theme-on-surface), var(--v-medium-emphasis-opacity));
}

.graph-types-toggle {
  width: 100%;
}

.graph-types-toggle :deep(.v-btn) {
  flex: 1;
}

.result-toolbar {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 8px;
}

.plot-container {
  flex: 1;
  min-height: 500px;
  overflow-x: auto;
  overflow-y: auto;
}

.plot-container :deep(figure.plot-figure) {
  width: fit-content;
  max-width: 100%;
  margin: 0 auto;
}

.plot-container svg {
  display: block;
}

.line-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 6px 16px;
  padding: 8px 12px;
  font-size: 12px;
  line-height: 1.4;
}

.line-legend-item {
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  white-space: nowrap;
}

.line-legend-item .swatch {
  display: inline-block;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  flex-shrink: 0;
}

.result-table {
  max-height: calc(100vh - 14rem);
  overflow-y: auto;
}
</style>

<style>
.line-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 6px 16px;
  padding: 8px 12px;
  font-size: 12px;
  line-height: 1.4;
}

.line-legend-item {
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  white-space: nowrap;
}

.line-legend-item .swatch {
  display: inline-block;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  flex-shrink: 0;
}
</style>
